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
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AvaloniaColor = Avalonia.Media.Color;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The fill behind a row is the tone of its kind: one step brighter under the pointer, and one more when the row is selected.
    public class ProjectTreeRowToneTests : IDisposable
    {
        private readonly List<Window> windows = new();
        private readonly CategoryPalette palette = CategoryPalette.FromApplication();

        public void Dispose()
        {
            foreach (var window in windows)
                window.Close();
        }

        private (Window Window, ProjectTreeView View, ProjectTree Tree) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            var window = new Window { Content = view, Width = 400, Height = 600 };
            windows.Add(window);
            window.Show();
            Settle(window);
            tree.SelectedNode = null;
            Settle(window);
            return (window, view, tree);
        }

        private static void Settle(Window window)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        private static Border RowOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node))
                .GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");

        private static AvaloniaColor FillOf(Border row) => ((ISolidColorBrush)row.Background!).Color;

        private CategoryTone ToneOf(TreeNode node) => palette.For(node.Category!.Value, node.ComposableDepth, node.KindDepth);

        private static IEnumerable<TreeNode> All(IEnumerable<TreeNode> nodes) =>
            nodes.SelectMany(node => new[] { node }.Concat(All(node.Children)));

        private static Point CenterOf(Window window, Border row) =>
            row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window)!.Value;

        [AvaloniaFact]
        public void ARowAtRest_HasTheFillOfItsKind()
        {
            var (_, view, tree) = Show();

            foreach (var node in All(tree.Roots))
                Assert.Equal(ToneOf(node).Fill, FillOf(RowOf(view, node)));
        }

        [AvaloniaFact]
        public void TheRowsOfTwoKinds_HaveTwoFills_AndTheSameKindHasOneFill()
        {
            var (_, view, tree) = Show();
            var composition = tree.Roots[0];
            var layer = composition.Children[0];
            var entities = layer.Children.Single(child => child.Header == "Entities");
            var entity = entities.Children[0];

            Assert.NotEqual(FillOf(RowOf(view, layer)), FillOf(RowOf(view, entities)));
            Assert.Equal(FillOf(RowOf(view, entities)), FillOf(RowOf(view, entity)));
        }

        [AvaloniaFact]
        public void ASelectedRow_HasTheSelectedFill_AndTheOthersKeepTheirs()
        {
            var (window, view, tree) = Show();
            var layer = tree.Roots[0].Children[0];
            var other = tree.Roots[0].Children[1];

            tree.SelectedNode = layer;
            Settle(window);

            Assert.Equal(ToneOf(layer).FillSelected, FillOf(RowOf(view, layer)));
            Assert.Equal(ToneOf(other).Fill, FillOf(RowOf(view, other)));
        }

        [AvaloniaFact]
        public void ARowUnderThePointer_HasTheHoverFill_AndASelectedRowKeepsTheSelectedFill()
        {
            var (window, view, tree) = Show();
            var first = tree.Roots[0].Children[0];
            var second = tree.Roots[0].Children[1];

            window.MouseMove(CenterOf(window, RowOf(view, second)));
            Settle(window);
            Assert.Equal(ToneOf(second).FillHover, FillOf(RowOf(view, second)));
            Assert.Equal(ToneOf(first).Fill, FillOf(RowOf(view, first)));

            tree.SelectedNode = second;
            Settle(window);
            Assert.Equal(ToneOf(second).FillSelected, FillOf(RowOf(view, second)));
        }

        [AvaloniaFact]
        public void EachStep_IsBrighterThanTheOneBefore()
        {
            var (_, _, tree) = Show();
            var tone = ToneOf(tree.Roots[0].Children[0]);

            static int Sum(AvaloniaColor color) => color.R + color.G + color.B;
            Assert.True(Sum(tone.Fill) < Sum(tone.FillHover));
            Assert.True(Sum(tone.FillHover) < Sum(tone.FillSelected));
        }
    }
}
