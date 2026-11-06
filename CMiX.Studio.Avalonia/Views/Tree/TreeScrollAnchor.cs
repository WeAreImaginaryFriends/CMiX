// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views
{
    // Keeps the first row in view at its place when a drop changes the rows above it.
    public sealed class TreeScrollAnchor
    {
        private readonly TreeView treeView;
        private readonly Func<ScrollViewer?> scroller;

        // scroller gives the scroll viewer of the tree.
        public TreeScrollAnchor(TreeView treeView, Func<ScrollViewer?> scroller)
        {
            this.treeView = treeView;
            this.scroller = scroller;
        }

        // The first row that is fully in view and is not the dragged node or under it, and its place in the view.
        public (TreeNode Node, double Top)? CaptureAnchor(TreeNode dragged)
        {
            if (scroller() is not { } view)
                return null;

            foreach (var item in treeView.GetVisualDescendants().OfType<TreeViewItem>())
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
            var item = TreeRows.FindItem(treeView, kept.Node);
            if (item == null || TreeRows.RowTopIn(item, view) is not { } top)
                return;

            var maxOffset = Math.Max(0, view.Extent.Height - view.Viewport.Height);
            view.Offset = view.Offset.WithY(Math.Clamp(view.Offset.Y + top - kept.Top, 0, maxOffset));
        }
    }
}
