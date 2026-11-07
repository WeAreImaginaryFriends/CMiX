// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views
{
    // The drag of a row of the project tree with the pointer. The tree captures the pointer when the drag begins, so the moves,
    // the wheel and the release come as normal events on every platform, and the tree draws its own cursor.
    // The tree view calls Press, Move, Release, Cancel and Wheel from its pointer and key events.
    // A file drag from the file manager is not a row drag: the tree takes it as a drop target (see ProjectTreeView).
    public sealed class TreeRowDrag
    {
        // A press that moves this far starts the drag. It is the token DragThreshold. A test pins the two.
        public const double Threshold = 4.0;

        // The cursor of a row drag. It stays the same over a blocked row: the frame of the row tells, not the cursor.
        // The system cursor for a drop that is not possible does not fit the look of the app.
        public const StandardCursorType MoveCursorType = StandardCursorType.DragMove;

        private readonly TreeView treeView;
        private readonly Func<ProjectTree?> tree;
        private readonly TreeDropMarker marker;
        private readonly Func<DragAutoScroller> autoScroller;
        private readonly TreeScrollAnchor scrollAnchor;
        private readonly Func<ScrollViewer?> scroller;

        private TreeNode? pressed;
        private Point pressPoint;
        private TreeNode? source;
        private IPointer? pointer;
        private Cursor? cursorBefore;
        private Control? blockTip;
        private readonly TreeDragGhost ghost = new();

        // tree gives the view model of the tree. autoScroller gives the scroller of the drag. scroller gives the scroll viewer of the tree.
        internal TreeRowDrag(TreeView treeView, Func<ProjectTree?> tree, TreeDropMarker marker, Func<DragAutoScroller> autoScroller,
            TreeScrollAnchor scrollAnchor, Func<ScrollViewer?> scroller)
        {
            this.treeView = treeView;
            this.tree = tree;
            this.marker = marker;
            this.autoScroller = autoScroller;
            this.scrollAnchor = scrollAnchor;
            this.scroller = scroller;
        }

        // True from the start of the drag to its end.
        public bool IsDragging => source != null;

        // True while the drop moves the item. The view keeps its scroll where it is (see ProjectTreeView).
        public bool IsDropping { get; private set; }

        // The cursor of the tree during the drag.
        public Cursor MoveCursor { get; } = new(MoveCursorType);

        // The left button pressed a row. The node is null when the row cannot be dragged. The position is in the tree view.
        public void Press(TreeNode? node, Point position)
        {
            pressed = node;
            pressPoint = position;
        }

        // A move past the threshold begins the drag. A move during the drag marks the place of the drop under the pointer.
        public void Move(Point position, PointerEventArgs e)
        {
            if (source == null)
            {
                if (pressed == null)
                    return;

                var distance = position - pressPoint;
                if (Math.Abs(distance.X) < Threshold && Math.Abs(distance.Y) < Threshold)
                    return;

                Begin(pressed, e.Pointer, position);
            }

            Track(position);
        }

        // The button is up: the row drops at the marked place, and the drag ends.
        public void Release(Point position)
        {
            pressed = null;
            if (source is { } node)
                _ = DropAsync(node, position);
        }

        // Ends the drag without a drop: Escape, or the pointer capture is lost.
        public void Cancel()
        {
            pressed = null;
            End();
        }

        // The wheel during the drag: positive steps scroll up (see DragAutoScroller.ScrollByWheel).
        public void Wheel(int steps)
        {
            if (source != null)
                autoScroller().ScrollByWheel(steps);
        }

        // The tree shows every row that can take the node for as long as the drag goes on.
        private void Begin(TreeNode node, IPointer pointer, Point position)
        {
            pressed = null;
            source = node;
            this.pointer = pointer;
            tree()?.BeginDrag(node);
            pointer.Capture(treeView);
            cursorBefore = treeView.Cursor;
            treeView.Cursor = MoveCursor;
            ShowGhost(node, position);
        }

        // The copy of the dragged row that follows the pointer. A test reads it.
        internal TreeDragGhost Ghost => ghost;

        private void ShowGhost(TreeNode node, Point position)
        {
            var item = treeView.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(row => ReferenceEquals(row.DataContext, node));
            if (item != null && TreeRows.RowBorderOf(item) is { } row)
                ghost.Show(row, treeView, position);
        }

        // Marks the place of the drop under the pointer and keeps the scroll of the drag going.
        private void Track(Point position)
        {
            ghost.Move(treeView, position);
            var (hovered, item, target, place) = Read(position);
            ShowBlockReason(hovered);
            if (item != null && target != null && place != null && CanDrop(target, place.Value))
                marker.Mark(item, place);
            else
                marker.Mark(null, null);

            autoScroller().Track(scroller() is { } view ? treeView.TranslatePoint(position, view)?.Y : null);
        }

        private bool CanDrop(TreeNode target, DropPosition position) =>
            source != null && tree() is { } project && project.CanDrop(source, target, position);

        // Drops the node, then ends the drag. The drop is one undo step, and it tells the user why a move is blocked (see ProjectTree.DropAsync).
        private async Task DropAsync(TreeNode node, Point position)
        {
            try
            {
                var drop = StartDrop(node, position);
                End();
                if (drop != null)
                    await drop;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ProjectTreeView: the drop failed. {exception}");
            }
            finally
            {
                // The drag ends also when the drop failed before End. A later drag is not touched.
                if (ReferenceEquals(source, node))
                    End();
            }
        }

        // Starts the drop at the place under the pointer. Null when no drop is offered there.
        // A drop changes the number of rows above and under the view. The tree must not jump: the first row in view stays where it
        // is, and the tree does not scroll to the moved row. The move is done before the first await of ProjectTree.DropAsync.
        private Task? StartDrop(TreeNode node, Point position)
        {
            var (_, _, target, place) = Read(position);
            if (tree() is not { } project || target == null || place == null || !CanDrop(target, place.Value))
                return null;

            var anchor = scrollAnchor.CaptureAnchor(node);
            IsDropping = true;
            try
            {
                return project.DropAsync(node, target, place.Value);
            }
            finally
            {
                IsDropping = false;
                scrollAnchor.RestoreAnchor(anchor);
                Dispatcher.UIThread.Post(() => scrollAnchor.RestoreAnchor(anchor), DispatcherPriority.Loaded);
            }
        }

        // Ends the drag: the rows get their look back, the scroll stops, the mark goes, the pointer is free and the cursor is back.
        private void End()
        {
            if (source == null)
                return;

            source = null;
            ShowBlockReason(null);
            tree()?.EndDrag();
            autoScroller().StopNow();
            marker.Mark(null, null);
            ghost.Hide();
            treeView.Cursor = cursorBefore;
            cursorBefore = null;

            var captured = pointer;
            pointer = null;
            if (ReferenceEquals(captured?.Captured, treeView))
                captured.Capture(null);
        }

        // The row under the pointer, the item that takes the drop, its node, and the place of the drop. The item that takes the drop
        // is the row under the pointer or one of its parents (see ProjectTree.Locate). It has no place when no drop is offered here.
        // The pointer among the rows but on no row (between two rows, or left of a row): the drop stays where it was.
        private (TreeViewItem? Hovered, TreeViewItem? Item, TreeNode? Target, DropPosition? Position) Read(Point position)
        {
            var hoveredItem = TreeRows.ItemOf(treeView.InputHitTest(position));
            if (source == null || tree() is not { } project)
                return (hoveredItem, null, null, null);

            if (hoveredItem?.DataContext is not TreeNode hovered)
                return IsAmongTheRows(position) ? Marked(hoveredItem) : (hoveredItem, null, null, null);

            var header = TreeRows.RowBorderOf(hoveredItem);
            var fraction = header == null || header.Bounds.Height <= 0
                ? 0.5
                : (treeView.TranslatePoint(position, header)?.Y ?? 0) / header.Bounds.Height;

            if (fraction < 0 || fraction > 1)
                return Marked(hoveredItem);

            if (project.Locate(source, hovered, fraction) is not { } place)
                return (hoveredItem, null, null, null);

            return (hoveredItem, TreeRows.ItemOfNode(hoveredItem, place.Target), place.Target, place.Position);
        }

        // The place that the marker keeps.
        private (TreeViewItem? Hovered, TreeViewItem? Item, TreeNode? Target, DropPosition? Position) Marked(TreeViewItem? hovered) =>
            marker.Marked is { DataContext: TreeNode node } marked && marker.MarkedPosition is { } position
                ? (hovered, marked, node, position)
                : (hovered, null, null, null);

        // True when the point is in the panel of the rows: on a row, between two rows, or left of a row. The room under the last row is not in it.
        private bool IsAmongTheRows(Point position) =>
            treeView.ItemsPanelRoot is { } panel
            && treeView.TranslatePoint(position, panel) is { } point
            && new Rect(panel.Bounds.Size).Contains(point);

        // The drag sends no pointer events to the rows, so the tooltip of a blocked row under the pointer opens from here. Null closes it.
        private void ShowBlockReason(TreeViewItem? hovered)
        {
            var row = hovered?.DataContext is TreeNode { IsDropBlocked: true } ? TreeRows.RowBorderOf(hovered) : null;
            if (ReferenceEquals(row, blockTip))
                return;

            if (blockTip != null)
                ToolTip.SetIsOpen(blockTip, false);

            blockTip = row;
            if (row != null)
                ToolTip.SetIsOpen(row, true);
        }
    }
}
