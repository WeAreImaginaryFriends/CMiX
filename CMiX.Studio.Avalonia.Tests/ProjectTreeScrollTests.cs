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
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tree does not scroll when the user clicks a row, so a double click works at the edge of the view. A row that a program
    // selects is brought into view.
    public class ProjectTreeScrollTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        // A composition with twelve layers: a tree that is much higher than the window.
        private (ProjectTreeView View, ProjectTree Tree, ScrollViewer Scroller) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            for (var i = 0; i < 12; i++)
                composition.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 500 };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            tree.SelectedNode = null;
            Settle();
            return (view, tree, view.GetVisualDescendants().OfType<ScrollViewer>().First());
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

        private double RowTop(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder").TranslatePoint(new Point(0, 0), window!)!.Value.Y;

        private Point RowPoint(ProjectTreeView view, TreeNode node)
        {
            var row = ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            return row.TranslatePoint(new Point(row.Bounds.Width - 20, row.Bounds.Height / 2), window!)!.Value;
        }

        // Scrolls until a layer row is near the bottom edge of the view, where its whole block is not in view. Returns that layer.
        private TreeNode ScrollToARowAtTheBottomEdge(ProjectTreeView view, ProjectTree tree, ScrollViewer scroller)
        {
            for (var offset = 0; offset < 900; offset += 10)
            {
                scroller.Offset = new Vector(0, offset);
                Settle();
                var found = tree.Roots[0].Children.FirstOrDefault(layer => RowTop(view, layer) is > 400 and < 440);
                if (found != null)
                    return found;
            }

            throw new InvalidOperationException("No row near the bottom edge.");
        }

        [AvaloniaFact]
        public void AClickOnARowAtTheEdgeOfTheView_DoesNotScrollTheTree()
        {
            var (view, tree, scroller) = Show();
            var layer = ScrollToARowAtTheBottomEdge(view, tree, scroller);
            var before = scroller.Offset;

            var point = RowPoint(view, layer);
            window!.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Settle();

            Assert.Same(layer, tree.SelectedNode);
            Assert.Equal(before, scroller.Offset);
        }

        [AvaloniaFact]
        public void ADoubleClickOnARowAtTheEdgeOfTheView_CollapsesIt_AndTheRowStaysWhereItWas()
        {
            var (view, tree, scroller) = Show();
            var layer = ScrollToARowAtTheBottomEdge(view, tree, scroller);
            var top = RowTop(view, layer);
            var offset = scroller.Offset;

            var point = RowPoint(view, layer);
            window!.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Settle();

            Assert.False(layer.IsExpanded);
            Assert.Equal(offset, scroller.Offset);
            Assert.Equal(top, RowTop(view, layer));
        }

        [AvaloniaFact]
        public void ARowThatAProgramSelectsFarAway_IsBroughtIntoView()
        {
            var (view, tree, scroller) = Show();
            var far = tree.Roots[0].Children[10];
            Assert.True(RowTop(view, far) > 500, "The row is not out of the view.");

            tree.SelectedNode = far;
            Settle();
            Dispatcher.UIThread.RunJobs();
            Settle();

            Assert.True(scroller.Offset.Y > 0);
            Assert.InRange(RowTop(view, far), 0, 500);
        }

        private void DragAt(ProjectTreeView view, ProjectTree tree, ScrollViewer scroller, double y, RawDragEventType type)
        {
            var data = new DataObject();
            data.Set(ProjectTreeView.NodeFormat, tree.Roots[0].Children[0]);
            var point = scroller.TranslatePoint(new Point(40, y), window!)!.Value;
            window!.DragDrop(point, type, data, DragDropEffects.Move);
            Settle();
        }

        [AvaloniaFact]
        public void ADragNearTheBottomEdge_ScrollsDown_AndADragNearTheTopEdge_ScrollsUp()
        {
            var (view, tree, scroller) = Show();
            tree.BeginDrag(tree.Roots[0].Children[0]);
            DragAt(view, tree, scroller, scroller.Bounds.Height / 2, RawDragEventType.DragEnter);
            DragAt(view, tree, scroller, scroller.Bounds.Height / 2, RawDragEventType.DragOver);
            DragAt(view, tree, scroller, scroller.Bounds.Height - 4, RawDragEventType.DragOver);
            DragAt(view, tree, scroller, scroller.Bounds.Height - 4, RawDragEventType.DragOver);
            Assert.True(view.AutoScrollStep() > 0);
            Assert.True(scroller.Offset.Y > 0);

            var down = scroller.Offset.Y;
            DragAt(view, tree, scroller, 4, RawDragEventType.DragOver);
            DragAt(view, tree, scroller, 4, RawDragEventType.DragOver);
            Assert.True(view.AutoScrollStep() < 0);
            Assert.True(scroller.Offset.Y < down);

            DragAt(view, tree, scroller, scroller.Bounds.Height + 20, RawDragEventType.DragLeave);
            tree.EndDrag();
        }

        [AvaloniaFact]
        public void ADragAwayFromTheEdges_DoesNotScroll_AndADragThatLeavesTheTree_Stops()
        {
            var (view, tree, scroller) = Show();
            tree.BeginDrag(tree.Roots[0].Children[0]);
            DragAt(view, tree, scroller, scroller.Bounds.Height / 2, RawDragEventType.DragEnter);

            DragAt(view, tree, scroller, scroller.Bounds.Height / 2, RawDragEventType.DragOver);
            Assert.Equal(0, view.AutoScrollStep());

            DragAt(view, tree, scroller, scroller.Bounds.Height - 4, RawDragEventType.DragOver);
            DragAt(view, tree, scroller, scroller.Bounds.Height - 4, RawDragEventType.DragOver);
            Assert.True(view.AutoScrollStep() > 0);
            DragAt(view, tree, scroller, scroller.Bounds.Height + 20, RawDragEventType.DragLeave);
            Assert.Equal(0, view.AutoScrollStep());
            tree.EndDrag();
        }
    }
}
