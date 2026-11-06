// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views
{
    // Shows the place of a drop on the rows of the project tree with pseudo classes. It keeps the marked row and its place.
    public sealed class TreeDropMarker
    {
        private const string DropBefore = ":dropbefore";
        private const string DropAfter = ":dropafter";
        private const string DropTarget = ":droptarget";
        private const string DropGap = ":dropgap";
        private const string DropTop = ":droptop";

        private readonly TreeView treeView;
        private TreeViewItem? barItem;

        public TreeDropMarker(TreeView treeView)
        {
            this.treeView = treeView;
        }

        public TreeViewItem? Marked { get; private set; }

        public DropPosition? MarkedPosition { get; private set; }

        // Marks the item for a drop at the position. A null item or position removes the mark.
        public void Mark(TreeViewItem? item, DropPosition? position)
        {
            if (Marked != null)
            {
                var old = (IPseudoClasses)Marked.Classes;
                old.Set(DropBefore, false);
                old.Set(DropAfter, false);
                old.Set(DropTarget, false);
            }

            if (barItem != null)
            {
                var oldBar = (IPseudoClasses)barItem.Classes;
                oldBar.Set(DropGap, false);
                oldBar.Set(DropTop, false);
                barItem = null;
            }

            Marked = item;
            MarkedPosition = position;
            if (item == null || position == null)
                return;

            ((IPseudoClasses)item.Classes).Set(
                position == DropPosition.Before ? DropBefore : position == DropPosition.After ? DropAfter : DropTarget,
                true);

            if (position == DropPosition.Inside || item.DataContext is not TreeNode node)
                return;

            // The bar is in the space under the row above the place: the last visible row of the block for a drop after the node.
            var above = position == DropPosition.After ? TreeDropService.LastVisible(node) : TreeDropService.RowAbove(node);
            var aboveItem = above == null ? null : TreeRows.ItemOfNode(item, above) ?? TreeRows.FindItem(treeView, above);
            barItem = aboveItem ?? item;
            ((IPseudoClasses)barItem.Classes).Set(aboveItem != null ? DropGap : DropTop, true);
        }
    }
}
