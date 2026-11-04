using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.Parts;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // On a screen with a scaling like 125%, every panel rounds its size to whole pixels, and the rows of a long tree are not
    // exactly one pitch apart. The bands follow the real rows, so a row is on its band at the top and at the bottom of the tree.
    public class ProjectTreeStripesScalingTests : IDisposable
    {
        private readonly List<Window> windows = new();

        public void Dispose()
        {
            foreach (var window in windows)
                window.Close();
        }

        // The window of the test gets a scaling of the screen. The headless window has no setting for it.
        private static void SetScaling(Window window, double scale)
        {
            var impl = window.PlatformImpl!;
            impl.GetType().GetField("<RenderScaling>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(impl, scale);
            ((Action<double>?)impl.GetType().GetProperty("ScalingChanged")!.GetValue(impl))?.Invoke(scale);
        }

        private static void Settle(Window window)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        private (Window Window, ProjectTreeView View, ProjectTree Tree) Show(double scale, int layers)
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            for (var i = 0; i < layers; i++)
                composition.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            var window = new Window { Content = view, Width = 400, Height = 900 };
            windows.Add(window);
            window.Show();
            SetScaling(window, scale);
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle(window);
            return (window, view, tree);
        }

        private static List<Border> RowsOf(ProjectTreeView view) =>
            view.GetVisualDescendants().OfType<TreeViewItem>()
                .Select(item => item.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder"))
                .Where(row => row.IsEffectivelyVisible)
                .ToList();

        private static double TopIn(Visual control, Visual root) => control.TranslatePoint(new Point(0, 0), root)!.Value.Y;

        // For each row that shows in the stripes: the opacity of the band that starts at the row, and how far that start is from the row.
        private static List<(double Opacity, double Gap)> BandsUnderTheShownRows(Window window, ProjectTreeView view)
        {
            var stripes = view.GetVisualDescendants().OfType<RowStripes>().Single();
            var bands = stripes.Bands();
            var result = new List<(double, double)>();
            foreach (var row in RowsOf(view))
            {
                var top = TopIn(row, stripes);
                if (top < 0 || top + 24 > stripes.Bounds.Height)
                    continue;

                var band = bands.OrderBy(b => Math.Abs(b.Band.Top - top)).First();
                result.Add((band.Opacity, band.Band.Top - top));
            }

            return result;
        }

        [AvaloniaTheory]
        [InlineData(1.0)]
        [InlineData(1.25)]
        [InlineData(1.5)]
        [InlineData(1.75)]
        [InlineData(2.0)]
        public void EveryShownRow_IsOnItsBand_AtTheTopOfALongTree(double scale)
        {
            var (window, view, _) = Show(scale, layers: 60);

            var bands = BandsUnderTheShownRows(window, view);

            Assert.True(bands.Count > 20, "The test needs many rows in view.");
            Assert.All(bands, band => Assert.InRange(band.Gap, -0.01, 0.01));
            for (var i = 1; i < bands.Count; i++)
                Assert.NotEqual(bands[i - 1].Opacity, bands[i].Opacity);
        }

        [AvaloniaTheory]
        [InlineData(1.0)]
        [InlineData(1.25)]
        [InlineData(1.5)]
        [InlineData(1.75)]
        public void EveryShownRow_IsOnItsBand_AtTheBottomOfALongTree(double scale)
        {
            var (window, view, _) = Show(scale, layers: 60);
            var viewer = view.GetVisualDescendants().OfType<ScrollViewer>().First();

            viewer.Offset = new Vector(0, viewer.Extent.Height);
            Settle(window);
            Assert.True(viewer.Offset.Y > 3000, "The tree did not scroll to the bottom.");
            var bands = BandsUnderTheShownRows(window, view);

            Assert.True(bands.Count > 20, "The test needs many rows in view.");
            Assert.All(bands, band => Assert.InRange(band.Gap, -0.01, 0.01));
            for (var i = 1; i < bands.Count; i++)
                Assert.NotEqual(bands[i - 1].Opacity, bands[i].Opacity);
        }

        [AvaloniaTheory]
        [InlineData(1.25)]
        [InlineData(1.5)]
        public void TheBandsOfAScrolledTree_KeepTheirParity_WhateverTheOffset(double scale)
        {
            var (window, view, tree) = Show(scale, layers: 60);
            var viewer = view.GetVisualDescendants().OfType<ScrollViewer>().First();
            var stripes = view.GetVisualDescendants().OfType<RowStripes>().Single();

            // The first row in view keeps its place in the order of all rows: the opacity of its band is that of its index.
            foreach (var offset in new[] { 0.0, 100.0, 1234.0, 2500.0 })
            {
                viewer.Offset = new Vector(0, offset);
                Settle(window);
                var all = RowsOf(view);
                var index = all.FindIndex(row => TopIn(row, stripes) >= 0);
                var top = TopIn(all[index], stripes);
                var band = stripes.Bands().OrderBy(b => Math.Abs(b.Band.Top - top)).First();

                Assert.Equal(index % 2 == 0 ? 1.0 : stripes.AlternateOpacity, band.Opacity);
            }
        }
    }
}
