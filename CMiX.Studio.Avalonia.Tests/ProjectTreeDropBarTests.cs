using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
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
    // A drag over a list of modifiers: the bar shows the place, the group "Modifiers" takes the row, and nothing is cut.
    public class ProjectTreeDropBarTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        // A composition with a layer, an entity, and three modifiers on it.
        private (ProjectTreeView View, ProjectTree Tree, Entity Entity, TreeNode Group) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)layer.ModelEntityManager.ManagerData.Items[0];
            for (var i = 0; i < 3; i++)
                entity.ModifierManager.AddItem(typeof(CMiX.Core.Modulation.Modifiers.TranslateModifier));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 900 };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            var group = tree.Roots[0].Children[0].Children[0].Children[0].Children.Single(child => child.Header == "Modifiers");
            return (view, tree, entity, group);
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

        private Point PointIn(ProjectTreeView view, TreeNode node, double fraction)
        {
            var row = ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            return row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height * fraction), window!)!.Value;
        }

        private void DragOver(ProjectTreeView view, TreeNode source, TreeNode target, double fraction)
        {
            var data = new DataObject();
            data.Set(ProjectTreeView.NodeFormat, source);
            var point = PointIn(view, target, fraction);
            window!.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Move);
            window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Move);
            Settle();
        }

        // The part of the bar that is drawn: the bar in the window, cut by everything around it.
        private static double SeenArea(Border line)
        {
            var seen = line.GetTransformedBounds();
            if (seen == null)
                return 0;

            var bounds = new Rect(seen.Value.Bounds.Size).TransformToAABB(seen.Value.Transform);
            var visible = bounds.Intersect(seen.Value.Clip);
            return visible.Width * visible.Height;
        }

        private static Border LineOf(TreeViewItem item) =>
            item.GetVisualDescendants().OfType<Border>().Last(border => border.Name == "DropLine");

        [AvaloniaFact]
        public void TheBarBeforeARowOfAList_IsSeen_ForTheFirstRowAndForTheOthers()
        {
            var (view, tree, _, group) = Show();
            var source = group.Children[0];
            tree.BeginDrag(source);

            // Before the third row. Before the second row is where the row is: no place, no bar.
            DragOver(view, source, group.Children[2], 0.2);
            var item = ItemOf(view, group.Children[2]);
            Assert.Contains(":dropbefore", item.Classes);
            Assert.True(SeenArea(LineOf(ItemOf(view, TreeDropService.LastVisible(group.Children[1])))) > 0, "The bar before the row is cut.");

            tree.EndDrag();
            tree.BeginDrag(group.Children[2]);
            DragOver(view, group.Children[2], group.Children[0], 0.2);
            Assert.Contains(":dropbefore", ItemOf(view, group.Children[0]).Classes);
            Assert.True(SeenArea(LineOf(ItemOf(view, group))) > 0, "The bar before the first row is cut.");
        }

        // The last row of the list has rows under it (its group of modulators): the bar after it is at the bottom of the item.
        [AvaloniaFact]
        public void TheBarAfterTheLastRowOfAList_IsSeen()
        {
            var (view, tree, _, group) = Show();
            var source = group.Children[0];
            var last = group.Children[2];
            tree.BeginDrag(source);

            DragOver(view, source, last.Children[0], 0.9);

            var item = ItemOf(view, last);
            Assert.Contains(":dropafter", item.Classes);
            Assert.True(SeenArea(LineOf(ItemOf(view, TreeDropService.LastVisible(last)))) > 0, "The bar after the last row is cut.");
        }

        [AvaloniaFact]
        public void ARowOfModifiers_CanBeDroppedOnTheGroupModifiers_AndGoesToTheEnd()
        {
            var (view, tree, entity, group) = Show();
            var source = group.Children[0];
            var moved = source.Item;
            var data = new DataObject();
            data.Set(ProjectTreeView.NodeFormat, source);
            tree.BeginDrag(source);
            Settle();

            Assert.True(group.IsDropCandidate);
            DragOver(view, source, group, 0.5);
            Assert.Contains(":droptarget", ItemOf(view, group).Classes);

            window!.DragDrop(PointIn(view, group, 0.5), RawDragEventType.Drop, data, DragDropEffects.Move);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(moved, entity.ModifierManager.ManagerData.Items[^1]);
            Assert.Equal(3, entity.ModifierManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void TheGroupModifiers_IsLit_WhileARowOfItsListIsDragged_AndTheRowsOfTheListAreNot()
        {
            var (view, tree, _, group) = Show();

            tree.BeginDrag(group.Children[0]);
            Settle();

            Assert.True(group.IsDropCandidate);
            Assert.All(group.Children, row => Assert.False(row.IsDropCandidate));
        }
    }
}
