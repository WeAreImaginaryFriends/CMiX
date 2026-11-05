using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The selected row has a thin border in the bright color of its kind. It is one pixel of the screen, and it takes no room.
    public class ProjectTreeSelectionFrameTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        private static void SetScaling(Window target, double scale)
        {
            var impl = target.PlatformImpl!;
            impl.GetType().GetField("<RenderScaling>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(impl, scale);
            ((Action<double>?)impl.GetType().GetProperty("ScalingChanged")!.GetValue(impl))?.Invoke(scale);
        }

        private void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window!.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        private (ProjectTreeView View, ProjectTree Tree) Show(double scale = 1.0)
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            ((Layer)composition.LayerManager.ManagerData.Items[0]).ModelEntityManager.AddItem(typeof(Entity));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 700 };
            window.Show();
            SetScaling(window, scale);
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            tree.SelectedNode = null;
            Settle();
            return (view, tree);
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static Border Part(ProjectTreeView view, TreeNode node, string name) =>
            ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == name);

        [AvaloniaFact]
        public void OnlyTheSelectedRow_HasTheFrame()
        {
            var (view, tree) = Show();
            var layer = tree.Roots[0].Children[0];
            var other = tree.Roots[0].Children[1];

            Assert.False(Part(view, layer, "SelectionFrame").IsEffectivelyVisible);

            tree.SelectedNode = layer;
            Settle();

            Assert.True(Part(view, layer, "SelectionFrame").IsEffectivelyVisible);
            Assert.False(Part(view, other, "SelectionFrame").IsEffectivelyVisible);
            Assert.False(Part(view, tree.Roots[0], "SelectionFrame").IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void TheFrame_IsTheBrightColorOfTheKind_OfTheRow()
        {
            var (view, tree) = Show();
            var palette = CategoryPalette.FromApplication();

            foreach (var node in new[] { tree.Roots[0], tree.Roots[0].Children[0], tree.Roots[0].Children[0].Children[0] })
            {
                tree.SelectedNode = node;
                Settle();

                var frame = Part(view, node, "SelectionFrame");
                Assert.Equal(palette.For(node.Category!.Value, node.ComposableDepth, node.KindDepth).KindSelected, ((ISolidColorBrush)frame.BorderBrush!).Color);
            }
        }

        [AvaloniaTheory]
        [InlineData(1.0)]
        [InlineData(1.25)]
        [InlineData(1.5)]
        [InlineData(2.0)]
        public void TheFrame_IsOnePixelOfTheScreen_AtEveryScaling(double scale)
        {
            var (view, tree) = Show(scale);
            var layer = tree.Roots[0].Children[0];
            tree.SelectedNode = layer;
            Settle();

            var thickness = Part(view, layer, "SelectionFrame").BorderThickness;

            Assert.Equal(1 / scale, thickness.Left, 6);
            Assert.Equal(thickness.Left, thickness.Top);
            Assert.Equal(thickness.Left, thickness.Right);
            Assert.Equal(thickness.Left, thickness.Bottom);
        }

        [AvaloniaFact]
        public void TheFrame_CoversTheRowExactly_AndTheRowDoesNotMoveWhenItIsSelected()
        {
            var (view, tree) = Show();
            var layer = tree.Roots[0].Children[0];
            var row = Part(view, layer, "RowBorder");
            var before = (row.Bounds, row.TranslatePoint(new Point(0, 0), window!));

            tree.SelectedNode = layer;
            Settle();

            var frame = Part(view, layer, "SelectionFrame");
            Assert.Equal(before, (row.Bounds, row.TranslatePoint(new Point(0, 0), window!)));
            Assert.Equal(row.Bounds.Size, frame.Bounds.Size);
            Assert.False(frame.IsHitTestVisible);
        }

        [AvaloniaFact]
        public void TheFrame_HasTheCornersOfTheRow()
        {
            var (view, tree) = Show();
            var layer = tree.Roots[0].Children[0];

            Assert.Equal(Part(view, layer, "RowBorder").CornerRadius, Part(view, layer, "SelectionFrame").CornerRadius);
        }
    }
}
