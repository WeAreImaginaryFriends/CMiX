// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Panels;

namespace CMiX.Studio.Avalonia.Views
{
    // Keeps the first row in view at its place when a drop changes the rows above it.
    public sealed class TreeScrollAnchor
    {
        private readonly TreeView treeView;
        private readonly TreeRowIndex rows;
        private readonly Func<ScrollViewer?> scroller;

        // scroller gives the scroll viewer of the tree.
        internal TreeScrollAnchor(TreeView treeView, TreeRowIndex rows, Func<ScrollViewer?> scroller)
        {
            this.treeView = treeView;
            this.rows = rows;
            this.scroller = scroller;
        }

        // The first row that is fully in view and is not the dragged node or under it, and its place in the view.
        public (TreeNode Node, double Top)? CaptureAnchor(TreeNode dragged)
        {
            if (scroller() is not { } view)
                return null;

            // A block that ends above the view has no row in view, so the rows under it are skipped too.
            bool Skip(TreeViewItem item) =>
                (item.DataContext is TreeNode node && ReferenceEquals(node, dragged))
                || (item.TranslatePoint(new Point(0, item.Bounds.Height), view) is { } bottom && bottom.Y < 0);

            foreach (var item in rows.InRowOrder(Skip))
            {
                if (item.DataContext is not TreeNode node || node.IsOrIsUnder(dragged))
                    continue;

                var top = TreeRows.RowTopIn(item, view);
                if (top is >= 0)
                    return (node, top.Value);
            }

            return null;
        }

        // Scrolls so that the anchor row is where it was.
        public void RestoreAnchor((TreeNode Node, double Top)? anchor)
        {
            if (anchor is not { } kept || scroller() is not { } view)
                return;

            treeView.UpdateLayout();
            Place(kept, view);
        }

        private void Place((TreeNode Node, double Top) kept, ScrollViewer view)
        {
            var item = rows.FindItem(kept.Node);
            if (item == null || TreeRows.RowTopIn(item, view) is not { } top)
                return;

            var maxOffset = Math.Max(0, view.Extent.Height - view.Viewport.Height);
            view.Offset = view.Offset.WithY(Math.Clamp(view.Offset.Y + top - kept.Top, 0, maxOffset));
        }

        // A row closes over a few frames (CollapsePanel), so the rows above the view get shorter after the close. The anchor row is
        // put back after each layout, until the list of children of every closed row is shut.
        public void HoldWhileClosing((TreeNode Node, double Top)? anchor, IReadOnlyList<TreeNode> closed)
        {
            if (anchor is not { } kept || closed.Count == 0 || scroller() is not { } view)
                return;

            var panels = closed.Select(node => rows.FindItem(node)?.GetVisualDescendants().OfType<CollapsePanel>().FirstOrDefault(panel => panel.Name == "PART_Collapse"))
                               .OfType<CollapsePanel>().ToList();
            if (panels.Count == 0)
                return;

            // A safe end, in case a panel never reaches its end.
            var passes = 0;
            void Keep(object? sender, EventArgs e)
            {
                var moving = panels.Any(panel => panel.Progress > 0) && ++passes < MaxHoldPasses;
                if (!moving)
                    treeView.LayoutUpdated -= Keep;

                Place(kept, view);
            }

            treeView.LayoutUpdated += Keep;
        }

        private const int MaxHoldPasses = 500;
    }
}
