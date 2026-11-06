using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Tests.Support;
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
            first.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));
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
            tree.Roots[0].Children[0].Children.Single(child => child.Group is EntitiesGroup).Children[0];

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
            var composition = tree.Roots[0];
            composition.IsExpanded = false;
            Settle();

            tree.BeginDrag(FirstEntity(tree));
            Settle();

            Assert.True(composition.IsDropHint);
            Assert.False(Part(view, composition, "DimLayer").IsEffectivelyVisible);
            Assert.Equal(ToneOf(composition).Fill, FillOf(view, composition));
        }

        // A modifier of an entity: a layer cannot hold it, so the layer rows are blocked.
        [AvaloniaFact]
        public void BlockedRow_DuringADrag_HasTheBlockedBorder()
        {
            var (view, tree) = Show();
            ((Entity)FirstEntity(tree).Item!).ModifierManager.AddItem(typeof(CMiX.Core.Modulation.Modifiers.TranslateModifier));
            Settle();
            var modifier = FirstEntity(tree).Children.Single(child => child.Group is ModifiersGroup).Children[0];
            var layer = tree.Roots[0].Children[1];
            Assert.False(Part(view, layer, "BlockedFrame").IsEffectivelyVisible);

            tree.BeginDrag(modifier);
            Settle();
            var frame = Part(view, layer, "BlockedFrame");

            Assert.True(layer.IsDropBlocked);
            Assert.True(frame.IsEffectivelyVisible);
            Assert.Equal(((ISolidColorBrush)Application.Current!.FindResource("DropBlockedBrush")!).Color, ((ISolidColorBrush)frame.BorderBrush!).Color);
            Assert.Equal((Thickness)view.FindResource("TreeSelectionThickness")!, frame.BorderThickness);
            Assert.True(frame.BorderThickness.Top > 0);
            Assert.False(frame.IsHitTestVisible);
            Assert.Equal(ToneOf(layer).Fill, FillOf(view, layer));
            Assert.False(Part(view, layer, "DimLayer").IsEffectivelyVisible);

            tree.EndDrag();
            Settle();
            Assert.False(Part(view, layer, "BlockedFrame").IsEffectivelyVisible);
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

        // The group that is lit takes the node: a drop on it moves the filter to the list of that group.
        [AvaloniaFact]
        public void ADropOnTheLitGroupOfFilters_MovesTheFilterThere()
        {
            var (view, tree) = Show();
            var firstLayer = tree.Roots[0].Children[0];
            var secondLayer = tree.Roots[0].Children[1];
            var filter = firstLayer.Children.Single(child => child.Group is FiltersGroup).Children[0];
            var target = secondLayer.Children.Single(child => child.Group is FiltersGroup);
            var lists = ((Layer)firstLayer.Item!, (Layer)secondLayer.Item!);
            var point = Center(view, target);

            TreeDrag.Over(window!, Center(view, filter), point);
            Assert.True(tree.IsDragging);
            Assert.Contains(":droptarget", ItemOf(view, target).Classes);
            Assert.Equal(ToneOf(target).FillSelected, FillOf(view, target));
            TreeDrag.Release(window!, point);

            Assert.Empty(lists.Item1.TextureModifierManager.ManagerData.Items);
            Assert.Single(lists.Item2.TextureModifierManager.ManagerData.Items);
        }

        // The center of the row of the node, in the window.
        private Point Center(ProjectTreeView view, TreeNode node)
        {
            var row = Part(view, node, "RowBorder");
            return row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window!)!.Value;
        }

        [AvaloniaFact]
        public void ADragWithThePointer_StartsTheDragOfTheTree_AndMarksTheRows()
        {
            var (view, tree) = Show();
            var source = FirstEntity(tree);
            var point = Center(view, source);

            TreeDrag.Over(window!, point, point + new Vector(30, 0));

            Assert.True(tree.IsDragging);
            Assert.True(source.IsDragSource);
            Assert.True(All(tree.Roots).Any(node => node.IsDropCandidate));

            // The pointer is released on the dragged row itself: no drop, the drag ends, and no row keeps a mark.
            TreeDrag.Release(window!, point + new Vector(30, 0));
            Assert.False(tree.IsDragging);
            Assert.All(All(tree.Roots), node => Assert.False(node.IsDragActive));
        }

        [AvaloniaFact]
        public void APressOnTheToggleColumn_NeverStartsADrag_EvenAwayFromTheButtons()
        {
            var (view, tree) = Show();
            var source = FirstEntity(tree);
            var toggles = ItemOf(view, source).GetVisualDescendants().OfType<RowToggles>().First();

            // The right end of the column: an entity has no mask toggle there, so it is the space around the buttons.
            var point = toggles.TranslatePoint(new Point(toggles.Bounds.Width - 3, toggles.Bounds.Height / 2), window!)!.Value;
            TreeDrag.Over(window!, point, point + new Vector(30, 0));

            var started = tree.IsDragging;
            TreeDrag.Release(window!, point + new Vector(30, 0));

            Assert.False(started);
        }
    }
}
