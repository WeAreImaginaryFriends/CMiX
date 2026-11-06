// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views
{
    // Finds the rows of the project tree: the item of a visual or of a node, and the place of a row.
    internal static class TreeRows
    {
        // The item that holds the source, or null.
        public static TreeViewItem? ItemOf(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is TreeViewItem item)
                    return item;

            return null;
        }

        public static TreeViewItem? FindItem(TreeView tree, TreeNode node) =>
            tree.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(item => ReferenceEquals(item.DataContext, node));

        // The item of a node: the item of the row under the pointer, or the first of its parent items that has the node.
        public static TreeViewItem? ItemOfNode(TreeViewItem from, TreeNode node)
        {
            for (var item = from; item != null; item = item.GetVisualAncestors().OfType<TreeViewItem>().FirstOrDefault())
                if (ReferenceEquals(item.DataContext, node))
                    return item;

            return null;
        }

        // The border of the row itself, without the rows under it.
        public static Border? RowBorderOf(TreeViewItem item) =>
            item.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Name == "RowBorder");

        // The top of the row in the coordinates of the scroll viewer.
        public static double? RowTopIn(TreeViewItem item, ScrollViewer scroller) =>
            RowBorderOf(item) is { } row ? row.TranslatePoint(new Point(0, 0), scroller)?.Y : null;
    }
}
