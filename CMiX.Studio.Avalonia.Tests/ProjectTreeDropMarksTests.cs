using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;
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
    // During a drag the rows that can take the node have a frame, the other rows are covered, and nothing moves.
    public class ProjectTreeDropMarksTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        // A composition with two layers. The first has two entities. The second has one.
        private (ProjectTreeView View, ProjectTree Tree) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            var first = (Layer)composition.LayerManager.ManagerData.Items[0];
            first.ModelEntityManager.AddItem(typeof(Entity));
            first.ModelEntityManager.AddItem(typeof(Entity));
            ((Layer)composition.LayerManager.ManagerData.Items[1]).ModelEntityManager.AddItem(typeof(Entity));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 700 };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            return (view, tree);
        }

        private void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window!.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static Border Part(ProjectTreeView view, TreeNode node, string name) =>
            ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == name);

        private static IEnumerable<TreeNode> All(IEnumerable<TreeNode> nodes) =>
            nodes.SelectMany(node => new[] { node }.Concat(All(node.Children)));

        private static TreeNode FirstEntity(ProjectTree tree) =>
            tree.Roots[0].Children[0].Children.Single(child => child.Header == "Entities").Children[0];

        private static AvaloniaColor FillOf(ProjectTreeView view, TreeNode node) =>
            ((ISolidColorBrush)Part(view, node, "RowBorder").Background!).Color;

        private static CategoryTone ToneOf(TreeNode node) =>
            CategoryPalette.FromApplication().For(node.Category!.Value, node.ComposableDepth, node.KindDepth);

        [AvaloniaFact]
        public void DuringADrag_ARowThatCanTakeTheNode_HasABrighterFill_AndARowThatCanNot_IsCovered()
        {
            var (view, tree) = Show();
            var source = FirstEntity(tree);

            tree.BeginDrag(source);
            Settle();

            foreach (var node in All(tree.Roots))
            {
                Assert.Equal(node.IsDropCandidate ? ToneOf(node).FillHover : ToneOf(node).Fill, FillOf(view, node));
                Assert.Equal(node.IsDimmed, Part(view, node, "DimLayer").IsEffectivelyVisible);
            }

            Assert.True(All(tree.Roots).Any(node => node.IsDropCandidate));
            Assert.True(All(tree.Roots).Any(node => node.IsDimmed));
            Assert.False(Part(view, source, "DimLayer").IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void WhenTheDragEnds_EveryMarkIsGone()
        {
            var (view, tree) = Show();
            tree.BeginDrag(FirstEntity(tree));
            Settle();

            tree.EndDrag();
            Settle();

            foreach (var node in All(tree.Roots))
            {
                Assert.Equal(ToneOf(node).Fill, FillOf(view, node));
                Assert.False(Part(view, node, "DimLayer").IsEffectivelyVisible);
            }
        }

        [AvaloniaFact]
        public void NoRow_HasABorder_DuringADrag()
        {
            var (view, tree) = Show();
            tree.BeginDrag(FirstEntity(tree));
            Settle();

            foreach (var node in All(tree.Roots))
            {
                var borders = ItemOf(view, node).GetVisualDescendants().OfType<Border>().Where(border => border.Name is not ("DropLine" or "SelectionFrame"));
                Assert.All(borders, border => Assert.Equal(new Thickness(0), border.BorderThickness));
            }

            Assert.DoesNotContain(All(tree.Roots), node => ItemOf(view, node).GetVisualDescendants().OfType<Border>().Any(border => border.Name is "CandidateFrame" or "HintFrame"));
        }

        [AvaloniaFact]
        public void ACollapsedRowWithACandidateUnderIt_IsNotCovered_AndHasNoOtherMark()
        {
            var (view, tree) = Show();
            var secondLayer = tree.Roots[0].Children[1];
            secondLayer.IsExpanded = false;
            Settle();

            tree.BeginDrag(FirstEntity(tree));
            Settle();

            Assert.True(secondLayer.IsDropHint);
            Assert.False(Part(view, secondLayer, "DimLayer").IsEffectivelyVisible);
            Assert.Equal(ToneOf(secondLayer).Fill, FillOf(view, secondLayer));
        }

        [AvaloniaFact]
        public void TheCover_UsesTheColorsOfTheTheme_AndTakesNoClick()
        {
            var (view, tree) = Show();
            var source = FirstEntity(tree);
            tree.BeginDrag(source);
            Settle();
            var cover = Part(view, tree.Roots[0], "DimLayer");

            Assert.Equal(((ISolidColorBrush)Application.Current!.FindResource("PanelBackgroundLevel1Brush")!).Color, ((ISolidColorBrush)cover.Background!).Color);
            Assert.Equal(Convert.ToDouble(Application.Current!.FindResource("TreeDimOpacity")), cover.Opacity);
            Assert.False(cover.IsHitTestVisible);
        }

        [AvaloniaFact]
        public void TheMarks_DoNotMoveOrResizeAnyRow()
        {
            var (view, tree) = Show();
            var before = All(tree.Roots).Select(node => (Part(view, node, "RowBorder").Bounds, Part(view, node, "RowBorder").TranslatePoint(new Point(0, 0), window!))).ToList();

            tree.BeginDrag(FirstEntity(tree));
            Settle();
            var during = All(tree.Roots).Select(node => (Part(view, node, "RowBorder").Bounds, Part(view, node, "RowBorder").TranslatePoint(new Point(0, 0), window!))).ToList();

            Assert.Equal(before, during);
        }

        [AvaloniaFact]
        public void ADragWithThePointer_StartsTheDragOfTheTree_AndMarksTheRows()
        {
            var (view, tree) = Show();
            var source = FirstEntity(tree);
            var row = Part(view, source, "RowBorder");
            var point = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window!)!.Value;

            window!.MouseDown(point, MouseButton.Left);
            window.MouseMove(point + new Vector(30, 0), RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();

            // The drag of the headless platform goes on: it has no pointer that drops. The end of the drag is tested on the tree.
            Assert.True(tree.IsDragging);
            Assert.True(source.IsDragSource);
            Assert.True(All(tree.Roots).Any(node => node.IsDropCandidate));

            // The pointer is released away from any row: the drag ends, so no row keeps a mark.
            window.MouseUp(point + new Vector(30, 0), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            Assert.False(tree.IsDragging);
            Assert.All(All(tree.Roots), node => Assert.False(node.IsDragActive));
        }
    }
}
