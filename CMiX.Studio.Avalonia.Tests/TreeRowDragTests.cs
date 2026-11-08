using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Tests.Support;
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
    // A row of the tree is dragged with the pointer: the tree captures the pointer, marks the place, scrolls with the wheel,
    // drops on the release and stops on Escape.
    public class TreeRowDragTests : IDisposable
    {
        private sealed class Screen
        {
            public Window Window { get; init; } = null!;
            public ProjectTreeView View { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Project Project { get; init; } = null!;
            public UndoManager Undo { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public TreeView TreeView => View.GetVisualDescendants().OfType<TreeView>().First();

            public ScrollViewer Scroller => View.GetVisualDescendants().OfType<ScrollViewer>().First();

            public TreeViewItem RowOf(TreeNode node) =>
                View.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

            public Border HeaderOf(TreeNode node) =>
                RowOf(node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");

            // A point in the header of a row, as a fraction of its height.
            public Point PointIn(TreeNode node, double fraction)
            {
                var header = HeaderOf(node);
                return header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height * fraction), Window)!.Value;
            }

            public void Settle()
            {
                Dispatcher.UIThread.RunJobs();
                Window.UpdateLayout();
                TreeDrag.Render(Window);
            }
        }

        private Window? window;

        public void Dispose() => window?.Close();

        // A holds the layers, B holds one. The window is high enough for every row, unless the layers are many.
        private Screen Show(int layersOfA = 2, double height = 1100)
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            project.CompositionManager.AddItem(typeof(Composition));
            var a = (Composition)project.CompositionManager.ManagerData.Items[0];
            var b = (Composition)project.CompositionManager.ManagerData.Items[1];
            for (var i = 0; i < layersOfA; i++)
                a.LayerManager.AddItem(typeof(Layer));
            b.LayerManager.AddItem(typeof(Layer));

            var undo = provider.GetRequiredService<UndoManager>();
            var tree = new ProjectTree(project, undo);
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 400, Height = height };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            var screen = new Screen { Window = window, View = view, Tree = tree, Project = project, Undo = undo, A = a, B = b };
            screen.Settle();
            return screen;
        }

        private static IEnumerable<TreeNode> AllNodes(IEnumerable<TreeNode> nodes) =>
            nodes.SelectMany(node => new[] { node }.Concat(AllNodes(node.Children)));

        private static TreeNode LastVisibleOf(TreeNode node) =>
            node.IsExpanded && node.Children.Count > 0 ? LastVisibleOf(node.Children[^1]) : node;

        // The pointer of the last pointer event of the window, and what it captured then.
        private static (Func<IPointer?> Pointer, Func<IInputElement?> Captured) WatchPointer(Window window)
        {
            IPointer? pointer = null;
            window.AddHandler(InputElement.PointerMovedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel, handledEventsToo: true);
            return (() => pointer, () => pointer?.Captured);
        }

        [AvaloniaFact]
        public void Move_PastTheThreshold_BeginsTheDragAndCapturesThePointer()
        {
            var screen = Show();
            var source = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var (_, captured) = WatchPointer(screen.Window);
            var start = screen.PointIn(source, 0.5);

            // The press itself gives the pointer to the name box of the row, not to the tree.
            screen.Window.MouseDown(start, MouseButton.Left);
            screen.Window.MouseMove(start + new Vector(0, TreeRowDrag.Threshold - 1), RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();
            Assert.False(screen.Tree.IsDragging);
            Assert.NotSame(screen.TreeView, captured());

            screen.Window.MouseMove(start + new Vector(0, 2 * TreeRowDrag.Threshold), RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();

            Assert.True(screen.View.RowDrag.IsDragging);
            Assert.True(screen.Tree.IsDragging);
            Assert.True(source.IsDragSource);
            Assert.Same(screen.TreeView, captured());
        }

        [AvaloniaFact]
        public void Move_OverARow_MarksTheDropPlace()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];

            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));

            Assert.Same(screen.RowOf(target), screen.View.Marker.Marked);
            Assert.Equal(DropPosition.Inside, screen.View.Marker.MarkedPosition);
            Assert.Contains(":droptarget", screen.RowOf(target).Classes);
        }

        [AvaloniaFact]
        public void Wheel_DuringADrag_ScrollsTheTree()
        {
            var screen = Show(layersOfA: 12, height: 500);
            var source = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var point = screen.PointIn(screen.Tree.CompositionNodes()[0].Children[0].Children[2], 0.5);
            TreeDrag.Over(screen.Window, screen.PointIn(source, 0.5), point);
            Assert.Equal(0, screen.Scroller.Offset.Y);

            TreeDrag.Wheel(screen.Window, point, -2);
            var down = screen.Scroller.Offset.Y;
            Assert.True(down > 0);

            TreeDrag.Wheel(screen.Window, point, 1);
            Assert.True(screen.Scroller.Offset.Y < down);
            Assert.True(screen.Tree.IsDragging);
        }

        [AvaloniaFact]
        public void Escape_DuringADrag_CancelsAndRestoresTheRows()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];
            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));
            Assert.True(screen.Tree.IsDragging);
            screen.Undo.Clear();

            TreeDrag.Cancel(screen.Window, screen.PointIn(target, 0.5));

            Assert.False(screen.View.RowDrag.IsDragging);
            Assert.False(screen.Tree.IsDragging);
            Assert.All(AllNodes(screen.Tree.Roots), node => Assert.False(node.IsDragActive));
            Assert.Null(screen.View.Marker.Marked);
            Assert.DoesNotContain(":droptarget", screen.RowOf(target).Classes);
            Assert.Null(screen.TreeView.Cursor);
            Assert.Equal(2, screen.A.LayerManager.ManagerData.Items.Count);
            Assert.Single(screen.B.LayerManager.ManagerData.Items);
            Assert.False(screen.Undo.CanUndo);
        }

        [AvaloniaFact]
        public void CaptureLost_DuringADrag_Cancels()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];
            var (pointer, _) = WatchPointer(screen.Window);
            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));
            Assert.True(screen.Tree.IsDragging);

            pointer()!.Capture(null);
            Dispatcher.UIThread.RunJobs();

            Assert.False(screen.View.RowDrag.IsDragging);
            Assert.False(screen.Tree.IsDragging);
            Assert.Null(screen.View.Marker.Marked);
            Assert.Null(screen.TreeView.Cursor);
            Assert.Equal(2, screen.A.LayerManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void Release_OverATarget_DropsAndSelectsTheItem()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var moved = layer.Item;
            var target = screen.Tree.CompositionNodes()[1];

            TreeDrag.Drop(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));

            Assert.Single(screen.A.LayerManager.ManagerData.Items);
            Assert.Same(moved, screen.B.LayerManager.ManagerData.Items[1]);
            Assert.Same(moved, screen.Tree.SelectedNode?.Item);
            Assert.Same(target.Children[0], screen.Tree.SelectedNode?.Parent);
            Assert.False(screen.View.RowDrag.IsDragging);
            Assert.False(screen.Tree.IsDragging);
            Assert.Null(screen.View.Marker.Marked);
            Assert.True(screen.Undo.CanUndo);
        }

        // A layer cannot hold a modifier of an entity: the layer row is blocked, with its reason in its tooltip, and takes no drop.
        [AvaloniaFact]
        public void Release_OverABlockedRow_DoesNothing()
        {
            var screen = Show();
            var layer = (Layer)screen.A.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)layer.ModelEntityManager.ManagerData.Items[0];
            entity.ModifierManager.AddItem(typeof(CMiX.Core.Modulation.Modifiers.TranslateModifier));
            TestServiceProviderFactory.ExpandAll(screen.Tree);
            screen.Settle();
            var entityNode = screen.Tree.CompositionNodes()[0].Children[0].Children[0].Children.Single(child => child.Group is EntitiesGroup).Children[0];
            var modifier = entityNode.Children.Single(child => child.Group is ModifiersGroup).Children[0];
            var blocked = screen.Tree.CompositionNodes()[0].Children[0].Children[1];

            TreeDrag.Over(screen.Window, screen.PointIn(modifier, 0.5), screen.PointIn(blocked, 0.5));
            Assert.True(blocked.IsDropBlocked);
            Assert.True(ToolTip.GetIsOpen(screen.HeaderOf(blocked)));
            Assert.Null(screen.View.Marker.Marked);
            Assert.Same(screen.View.RowDrag.MoveCursor, screen.TreeView.Cursor);
            screen.Undo.Clear();

            TreeDrag.Release(screen.Window, screen.PointIn(blocked, 0.5));

            Assert.Single(entity.ModifierManager.ManagerData.Items);
            Assert.Empty(((Layer)screen.A.LayerManager.ManagerData.Items[1]).ModifierManager.ManagerData.Items);
            Assert.False(screen.Tree.IsDragging);
            Assert.False(ToolTip.GetIsOpen(screen.HeaderOf(blocked)));
            Assert.False(screen.Undo.CanUndo);
        }

        // The pointer goes from the bottom half of the last row of B to the space under that row: the drop after B stays, and the release takes it.
        [AvaloniaFact]
        public void Release_BetweenTwoRows_UsesTheMarkedRow()
        {
            var screen = Show();
            var nodeA = screen.Tree.CompositionNodes()[0];
            var nodeB = screen.Tree.CompositionNodes()[1];
            var lastRow = LastVisibleOf(nodeB);
            TreeDrag.Over(screen.Window, screen.PointIn(nodeA, 0.5), screen.PointIn(lastRow, 0.9));
            Assert.Same(screen.RowOf(nodeB), screen.View.Marker.Marked);
            Assert.Equal(DropPosition.After, screen.View.Marker.MarkedPosition);

            var gap = Convert.ToDouble(screen.View.Resources["TreeRowGap"]);
            var header = screen.HeaderOf(lastRow);
            var between = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height + gap / 2), screen.Window)!.Value;
            TreeDrag.MoveTo(screen.Window, between);
            Assert.Same(screen.RowOf(nodeB), screen.View.Marker.Marked);
            Assert.Equal(DropPosition.After, screen.View.Marker.MarkedPosition);

            TreeDrag.Release(screen.Window, between);

            Assert.Equal(new IControl[] { screen.B, screen.A }, screen.Project.CompositionManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void Drag_OfARow_ShowsACopyOfTheRowUnderThePointerUntilTheEnd()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];
            var ghost = screen.View.RowDrag.Ghost;
            Assert.False(ghost.IsShown);

            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));

            Assert.True(ghost.IsShown);
            Assert.False(ghost.Ghost.IsHitTestVisible);
            Assert.Equal(screen.RowOf(layer).Bounds.Width > 0, ghost.Ghost.Width > 0);
            Assert.True(ghost.Ghost.Child is global::Avalonia.Controls.Image || ghost.Ghost.Background is VisualBrush);

            var pointer = screen.PointIn(target, 0.5);
            TreeDrag.Cancel(screen.Window, pointer);

            Assert.False(ghost.IsShown);
        }

        [AvaloniaTheory]
        [InlineData(0.15)]
        [InlineData(0.5)]
        [InlineData(0.7)]
        public void Drag_OfARow_KeepsTheGrabbedPointOfTheRowUnderThePointer(double along)
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];
            var header = screen.HeaderOf(layer);
            var row = new Rect(header.TranslatePoint(default, screen.Window)!.Value, header.Bounds.Size);
            var press = new Point(row.X + row.Width * along, row.Y + row.Height / 2);
            var pointer = screen.PointIn(target, 0.5);

            TreeDrag.Over(screen.Window, press, pointer);

            // The copy has the size of the row, and the point that was pressed is where the pointer is.
            var ghost = screen.View.RowDrag.Ghost.Ghost;
            var copy = new Rect(ghost.TranslatePoint(default, screen.Window)!.Value, ghost.Bounds.Size);
            Assert.Equal(row.Width, copy.Width, 1);
            Assert.Equal(row.Height, copy.Height, 1);
            Assert.Equal(pointer.X - (press.X - row.X), copy.X, 1.5);
            Assert.Equal(pointer.Y - (press.Y - row.Y), copy.Y, 1.5);
            TreeDrag.Cancel(screen.Window, pointer);
        }

        [AvaloniaFact]
        public void Drag_OfARow_TheCopyThatTheWindowShowsHasTheFillTheIconAndTheNameOfTheRow()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];

            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));

            // The window is drawn as the user sees it, and the pixels of the copy are read back. The copy is made opaque for the test, so
            // the rows behind it do not mix in.
            var ghost = screen.View.RowDrag.Ghost.Ghost;
            ghost.Opacity = 1;
            screen.Window.UpdateLayout();
            var size = new PixelSize((int)Math.Ceiling(screen.Window.Bounds.Width), (int)Math.Ceiling(screen.Window.Bounds.Height));
            var bitmap = new global::Avalonia.Media.Imaging.RenderTargetBitmap(size, new Vector(96, 96));
            bitmap.Render(screen.Window);
            using var stream = new MemoryStream();
            bitmap.Save(stream);
            stream.Position = 0;
            using var decoded = global::Avalonia.Media.Imaging.WriteableBitmap.Decode(stream);
            using var pixels = decoded.Lock();
            int Pixel(int x, int y) => System.Runtime.InteropServices.Marshal.ReadInt32(pixels.Address, y * pixels.RowBytes + x * 4);

            var origin = ghost.TranslatePoint(default, screen.Window)!.Value;
            var left = (int)Math.Round(origin.X);
            var top = (int)Math.Round(origin.Y);
            var width = (int)Math.Round(ghost.Bounds.Width);
            var height = (int)Math.Round(ghost.Bounds.Height);
            var middle = top + height / 2;
            var fill = Pixel(left + 2, middle);

            // From edge to edge the copy has the fill, so nothing is cut at the right.
            Assert.Equal(fill, Pixel(left + width - 3, middle));

            int Different(int from, int to)
            {
                var count = 0;
                for (var x = left + from; x < left + to; x++)
                    for (var y = top; y < top + height; y++)
                        if (Pixel(x, y) != fill)
                            count++;
                return count;
            }

            // The icon is at the left of the row, and the name is after it.
            Assert.True(Different(4, 24) > 10, "The icon is not in the copy.");
            Assert.True(Different(26, 120) > 20, "The name is not in the copy.");

            TreeDrag.Cancel(screen.Window, screen.PointIn(target, 0.5));
        }
        [AvaloniaFact]
        public void Drag_OfARow_FadesTheRowThatIsDraggedAndRestoresItAtTheEnd()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var other = screen.Tree.CompositionNodes()[0].Children[0].Children[1];
            var target = screen.Tree.CompositionNodes()[1];
            Assert.Equal(1, screen.HeaderOf(layer).Opacity);

            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));

            Assert.Equal(0.4, screen.HeaderOf(layer).Opacity);
            Assert.Equal(1, screen.HeaderOf(other).Opacity);

            TreeDrag.Cancel(screen.Window, screen.PointIn(target, 0.5));

            Assert.Equal(1, screen.HeaderOf(layer).Opacity);
        }

        [AvaloniaFact]
        public void Cursor_DuringADrag_IsTheMoveCursor()
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var target = screen.Tree.CompositionNodes()[1];
            Assert.Null(screen.TreeView.Cursor);

            TreeDrag.Over(screen.Window, screen.PointIn(layer, 0.5), screen.PointIn(target, 0.5));
            Assert.Equal(StandardCursorType.DragMove, TreeRowDrag.MoveCursorType);
            Assert.Same(screen.View.RowDrag.MoveCursor, screen.TreeView.Cursor);

            // Over a row that takes no drop (the dragged row itself) the cursor stays the move cursor.
            TreeDrag.MoveTo(screen.Window, screen.PointIn(layer, 0.5));
            Assert.Null(screen.View.Marker.Marked);
            Assert.Same(screen.View.RowDrag.MoveCursor, screen.TreeView.Cursor);

            TreeDrag.Release(screen.Window, screen.PointIn(layer, 0.5));
            Assert.Null(screen.TreeView.Cursor);
        }

        [AvaloniaFact]
        public void TreeRowDrag_Threshold_EqualsTheToken()
        {
            Assert.Equal(TreeRowDrag.Threshold, Convert.ToDouble(Application.Current!.FindResource("DragThreshold")));
        }
    }
}
