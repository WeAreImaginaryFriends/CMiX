// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
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

        // A collapsed row with a place for the node under it opens when the pointer rests on it this long (see OpensOnRest).
        // A test can change it.
        public static readonly TimeSpan DefaultRestDelay = TimeSpan.FromSeconds(0.6);
        internal TimeSpan RestDelay { get; set; } = DefaultRestDelay;

        // The row that the pointer rests on, and the timer that opens it.
        private TreeNode? resting;
        private DispatcherTimer? restTimer;

        // The rows that this drag opened, in the order that they opened. A new drag starts a new list.
        private readonly List<TreeNode> openedByDrag = new();
        internal IReadOnlyList<TreeNode> OpenedByDrag => openedByDrag;

        // The row that waits to open, or null, and the time that the timer waits. A test reads them.
        internal TreeNode? RestingNode => resting;
        internal TimeSpan? RestTimerInterval => restTimer is { IsEnabled: true } timer ? timer.Interval : null;

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
            openedByDrag.Clear();

            // The picture is taken before the rows change their look for the drag, so it is the row as the user pressed it.
            ShowGhost(node, position);
            tree()?.BeginDrag(node);
            pointer.Capture(treeView);
            cursorBefore = treeView.Cursor;
            treeView.Cursor = MoveCursor;
        }

        // The label of the dragged row that follows the pointer. A test reads it.
        internal TreeDragGhost Ghost => ghost;

        private void ShowGhost(TreeNode node, Point position)
        {
            var item = treeView.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(row => ReferenceEquals(row.DataContext, node));
            if (item != null && TreeRows.RowBorderOf(item) is Border row)
                ghost.Show(row, node.IconKey, node.Header, treeView, position);
        }

        // Marks the place of the drop under the pointer and keeps the scroll of the drag going.
        // The label of the drag moves after the mark, so it can keep clear of the line of the drop.
        private void Track(Point position)
        {
            var (hovered, item, target, place) = Read(position);
            ShowBlockReason(hovered);
            TrackRest(hovered);
            if (item != null && target != null && place != null && CanDrop(target, place.Value))
                marker.Mark(item, place);
            else
                marker.Mark(null, null);

            ghost.Move(treeView, position, DropLineTop());
            autoScroller().Track(scroller() is { } view ? treeView.TranslatePoint(position, view)?.Y : null);
        }

        // The top of the line of the drop between two rows, in the coordinates of the tree view, or null when no line shows. A line in the
        // space under a row starts at the bottom of the row. A line at the top of a row starts at the top of its item.
        private double? DropLineTop()
        {
            if (marker.Line is not { } line)
                return null;

            if (!line.Below)
                return line.Item.TranslatePoint(default, treeView)?.Y;

            return TreeRows.RowBorderOf(line.Item) is { } row ? row.TranslatePoint(new Point(0, row.Bounds.Height), treeView)?.Y : null;
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
                // The rows that the drag opened close in the same anchor, so the first row in view stays where it is. The target and
                // its parents stay open: they show where the node went, also for a drop before or after a child of an opened row.
                var closed = CloseOpenedRows(target);
                scrollAnchor.RestoreAnchor(anchor);
                scrollAnchor.HoldWhileClosing(anchor, closed);
                Dispatcher.UIThread.Post(() => scrollAnchor.RestoreAnchor(anchor), DispatcherPriority.Loaded);
            }
        }

        // Ends the drag: the rows get their look back, the scroll stops, the mark goes, the pointer is free and the cursor is back.
        private void End()
        {
            if (source == null)
                return;

            // No drop closed them (a cancel, or a release where no drop is offered): every row that the drag opened closes, and the
            // first row in view stays where it is.
            if (openedByDrag.Count > 0)
            {
                var anchor = scrollAnchor.CaptureAnchor(source);
                var closed = CloseOpenedRows(null);
                scrollAnchor.RestoreAnchor(anchor);
                scrollAnchor.HoldWhileClosing(anchor, closed);
            }

            source = null;
            StopRest();
            hoveredNode = null;
            closedByKey = null;
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

        // The rest starts when the pointer comes on a row that opens on a rest, and goes on while the pointer moves inside that row.
        // Another row, or the end of the drag, stops it. A row that the key closed does not open on a rest until the pointer leaves it.
        private void TrackRest(TreeViewItem? hovered)
        {
            hoveredNode = hovered?.DataContext as TreeNode;
            if (!ReferenceEquals(hoveredNode, closedByKey))
                closedByKey = null;

            var node = hoveredNode != null && OpensOnRest(hoveredNode) && !ReferenceEquals(hoveredNode, closedByKey) ? hoveredNode : null;
            if (ReferenceEquals(node, resting))
                return;

            StopRest();
            resting = node;
            if (node == null)
                return;

            if (restTimer == null)
            {
                restTimer = new DispatcherTimer();
                restTimer.Tick += (_, _) => OpenRested();
            }

            restTimer.Interval = RestDelay;
            restTimer.Start();
        }

        // A collapsed row with a place for the node under it. The row can take the node itself too, such as a layer for an entity:
        // open, it shows the places between its children.
        private static bool OpensOnRest(TreeNode node) =>
            node is { IsDragActive: true, HasDropTargetBelow: true, IsExpanded: false, Children.Count: > 0 };

        private void StopRest()
        {
            restTimer?.Stop();
            resting = null;
        }

        // The row under the pointer during the drag, and the row that the key closed last.
        private TreeNode? hoveredNode;
        private TreeNode? closedByKey;

        // The key of Tree.Expand during the drag (see ProjectTree.ToggleDragRow). A collapsed row that opens on a rest opens at once.
        // An open row that this drag opened closes again. A row that the user opened before the drag stays open.
        internal void ToggleHovered()
        {
            if (source == null || hoveredNode is not { } node)
                return;

            if (OpensOnRest(node))
            {
                StopRest();
                closedByKey = null;
                node.IsExpanded = true;
                openedByDrag.Add(node);
            }
            else if (node.IsExpanded && openedByDrag.Remove(node))
            {
                StopRest();
                closedByKey = node;
                node.IsExpanded = false;
            }
        }

        // Closes the rows that the drag opened, except the kept node and its parents. The rows that were open before the drag are not
        // in the list, so they never close here.
        // Returns the rows that closed.
        private List<TreeNode> CloseOpenedRows(TreeNode? kept)
        {
            var closed = openedByDrag.Where(node => kept == null || !kept.IsOrIsUnder(node)).ToList();
            foreach (var node in closed)
                node.IsExpanded = false;

            openedByDrag.Clear();
            return closed;
        }

        // Opens the row that the pointer rests on. A test calls it to skip the wait.
        internal void OpenRested()
        {
            var node = resting;
            StopRest();
            if (source == null || node == null || !OpensOnRest(node))
                return;

            node.IsExpanded = true;
            openedByDrag.Add(node);
        }

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
