using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Tests.Support;
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
        private (ProjectTreeView View, ProjectTree Tree, ScrollViewer Scroller) Show(FakeDragWheelSource? wheelSource = null)
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            for (var i = 0; i < 12; i++)
                composition.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>(), null, wheelSource);
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
        public void ProgramSelection_RevealsTheRow()
        {
            var (view, tree, scroller) = Show();
            var far = tree.Roots[0].Children[10];
            Assert.True(RowTop(view, far) > 500, "The row is not out of the view.");

            tree.Select(far, SelectionOrigin.Program);
            Settle();
            Dispatcher.UIThread.RunJobs();
            Settle();

            Assert.True(scroller.Offset.Y > 0);
            Assert.InRange(RowTop(view, far), 0, 500);
        }

        [AvaloniaFact]
        public void PointerSelection_DoesNotScroll()
        {
            var (view, tree, scroller) = Show();
            var far = tree.Roots[0].Children[10];
            Assert.True(RowTop(view, far) > 500, "The row is not out of the view.");

            tree.Select(far, SelectionOrigin.Pointer);
            Settle();
            Dispatcher.UIThread.RunJobs();
            Settle();

            Assert.Equal(0, scroller.Offset.Y);
        }

        // A point of the scroll viewer, in the window.
        private Point PointAt(ScrollViewer scroller, double y) => scroller.TranslatePoint(new Point(40, y), window!)!.Value;

        // Starts the drag of the first layer with the pointer and moves it to the height y of the scroll viewer.
        private void DragTo(ProjectTreeView view, ProjectTree tree, ScrollViewer scroller, double y)
        {
            TreeDrag.Over(window!, RowPoint(view, tree.Roots[0].Children[0]), PointAt(scroller, y));
            Settle();
        }

        // Moves the pointer of the drag to the height y of the scroll viewer.
        private void MoveTo(ScrollViewer scroller, double y)
        {
            TreeDrag.MoveTo(window!, PointAt(scroller, y));
            Settle();
        }

        [AvaloniaFact]
        public void ADragNearTheBottomEdge_ScrollsDown_AndADragNearTheTopEdge_ScrollsUp()
        {
            var (view, tree, scroller) = Show();
            DragTo(view, tree, scroller, scroller.Bounds.Height / 2);
            MoveTo(scroller, scroller.Bounds.Height - 4);
            Assert.True(view.AutoScrollStep() > 0);
            Assert.True(scroller.Offset.Y > 0);

            var down = scroller.Offset.Y;
            MoveTo(scroller, 4);
            Assert.True(view.AutoScrollStep() < 0);
            Assert.True(scroller.Offset.Y < down);

            TreeDrag.Cancel(window!, PointAt(scroller, 4));
            Assert.False(tree.IsDragging);
        }

        [AvaloniaFact]
        public void TheMouseWheel_ScrollsTheTreeDuringADrag_AndNotWithoutOne()
        {
            var (view, tree, scroller) = Show();
            view.ScrollByWheel(-2);
            Assert.Equal(0, scroller.Offset.Y);

            DragTo(view, tree, scroller, scroller.Bounds.Height / 2);
            view.ScrollByWheel(-2);
            var down = scroller.Offset.Y;
            Assert.True(down > 0);

            view.ScrollByWheel(1);
            Assert.True(scroller.Offset.Y < down);

            TreeDrag.Cancel(window!, PointAt(scroller, scroller.Bounds.Height / 2));
        }

        // The pointer is captured, so the drag goes on beyond the edge of the tree. Escape stops it.
        [AvaloniaFact]
        public void ADragAwayFromTheEdges_DoesNotScroll_AndACancelledDrag_Stops()
        {
            var (view, tree, scroller) = Show();
            DragTo(view, tree, scroller, scroller.Bounds.Height / 2);
            Assert.Equal(0, view.AutoScrollStep());

            MoveTo(scroller, scroller.Bounds.Height - 4);
            Assert.True(view.AutoScrollStep() > 0);

            TreeDrag.Cancel(window!, PointAt(scroller, scroller.Bounds.Height - 4));
            Assert.Equal(0, view.AutoScrollStep());
            Assert.False(view.IsAutoScrolling);
        }

        // A file drag comes from the system (see ProjectTreeView.OnDragOver). The wheel source serves it.
        private void FilesAt(ScrollViewer scroller, double y, RawDragEventType type)
        {
            var data = new DataObject();
            data.Set(DataFormats.Files, Array.Empty<IStorageItem>());
            window!.DragDrop(PointAt(scroller, y), type, data, DragDropEffects.Copy);
            Settle();
        }

        [AvaloniaFact]
        public void DragLeave_AfterAFileDragOverNearTheEdge_StopsTheScrollAfterOneDispatcherPass()
        {
            var source = new FakeDragWheelSource();
            var (view, _, scroller) = Show(source);
            FilesAt(scroller, scroller.Bounds.Height / 2, RawDragEventType.DragEnter);
            FilesAt(scroller, scroller.Bounds.Height - 4, RawDragEventType.DragOver);
            Assert.True(view.IsAutoScrolling);
            Assert.True(source.IsActive);

            // A file drag that is cancelled ends with a leave inside the tree, and with no drop. The headless DragDrop runs the
            // dispatcher jobs, so the leave is raised here on a row.
            var data = new DataObject();
            data.Set(DataFormats.Files, Array.Empty<IStorageItem>());
            var row = view.GetVisualDescendants().OfType<TreeViewItem>().First();
            row.RaiseEvent(new DragEventArgs(DragDrop.DragLeaveEvent, data, row, new Point(40, 4), KeyModifiers.None));
            Assert.True(source.IsActive);

            Dispatcher.UIThread.RunJobs();
            Assert.False(view.IsAutoScrolling);
            Assert.False(source.IsActive);
        }

        // A row drag gets the wheel as a pointer event: the wheel source of the system stays off.
        [AvaloniaFact]
        public void ARowDrag_DoesNotStartTheWheelSource()
        {
            var source = new FakeDragWheelSource();
            var (view, tree, scroller) = Show(source);

            DragTo(view, tree, scroller, scroller.Bounds.Height - 4);

            Assert.True(view.IsAutoScrolling);
            Assert.Equal(0, source.StartCount);
            Assert.False(source.IsActive);
            TreeDrag.Cancel(window!, PointAt(scroller, scroller.Bounds.Height - 4));
        }
    }
}
