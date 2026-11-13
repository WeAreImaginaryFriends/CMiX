// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views
{
    // Starts a drag of a row of the project tree with a pressed button, and gives the points of the pointer to the drag
    // (see TreeDragSession). The tree captures the pointer when the drag begins, so the moves, the wheel and the release come as
    // normal events on every platform, and the tree draws its own cursor.
    // The tree view calls Press, Move, Release, Cancel and Wheel from its pointer and key events.
    // A file drag from the file manager is not a row drag: the tree takes it as a drop target (see ProjectTreeView).
    public sealed class TreeRowDrag
    {
        // A press that moves this far starts the drag. It is the token DragThreshold. A test pins the two.
        public const double Threshold = 4.0;

        public const StandardCursorType MoveCursorType = TreeDragSession.MoveCursorType;

        public static readonly TimeSpan DefaultRestDelay = TreeDragSession.DefaultRestDelay;

        private readonly TreeView treeView;
        private readonly TreeDragSession session;

        private TreeNode? pressed;
        private Point pressPoint;

        // The pointer that the tree captured for the drag. Null when this starter runs no drag.
        private IPointer? pointer;

        internal TreeRowDrag(TreeView treeView, TreeDragSession session)
        {
            this.treeView = treeView;
            this.session = session;
        }

        // True from the start of the drag with the button to its end. A drag that another starter began is not in it.
        public bool IsDragging => pointer != null && session.IsRunning;

        // The tests of the row drag read the parts of the drag from here.
        public Cursor MoveCursor => session.MoveCursor;
        internal TreeDragGhost Ghost => session.Ghost;
        internal IReadOnlyList<TreeNode> OpenedByDrag => session.OpenedRows;
        internal TreeNode? RestingNode => session.RestingNode;
        internal TimeSpan? RestTimerInterval => session.RestTimerInterval;
        internal void OpenRested() => session.OpenRested();

        // The left button pressed a row. The node is null when the row cannot be dragged. The position is in the tree view.
        public void Press(TreeNode? node, Point position)
        {
            pressed = node;
            pressPoint = position;
        }

        // A move past the threshold begins the drag. A move during the drag marks the place of the drop under the pointer.
        public void Move(Point position, PointerEventArgs e)
        {
            if (!IsDragging)
            {
                if (pressed == null || session.IsRunning)
                    return;

                var distance = position - pressPoint;
                if (Math.Abs(distance.X) < Threshold && Math.Abs(distance.Y) < Threshold)
                    return;

                Begin(pressed, e.Pointer, position);
            }

            session.Track(position);
        }

        // The button is up: the row drops at the marked place, and the drag ends.
        // The release can come at another point than the last move, so the place is read again at this point.
        public void Release(Point position)
        {
            pressed = null;
            if (!IsDragging)
                return;

            session.Aim(position);
            End(TreeDragEnd.Drop);
        }

        // Ends the drag without a drop: Escape, or the pointer capture is lost.
        public void Cancel()
        {
            pressed = null;
            End(TreeDragEnd.Cancel);
        }

        // The wheel during the drag: positive steps scroll up (see DragAutoScroller.ScrollByWheel).
        public void Wheel(int steps)
        {
            if (IsDragging)
                session.ScrollByWheel(steps);
        }

        private void Begin(TreeNode node, IPointer pointer, Point position)
        {
            pressed = null;
            this.pointer = pointer;
            session.Start(node, position);
            pointer.Capture(treeView);
        }

        // Ends the drag, then makes the pointer free. The release of the capture comes back as a Cancel. The drag is at its end
        // then, so that Cancel does nothing.
        private void End(TreeDragEnd end)
        {
            if (!IsDragging)
                return;

            session.End(end);

            var captured = pointer;
            pointer = null;
            if (ReferenceEquals(captured?.Captured, treeView))
                captured.Capture(null);
        }
    }
}
