// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;

namespace CMiX.Studio.Avalonia.Views
{
    // The mark of a drop that a row shows.
    public enum DropMark
    {
        // The row shows no mark.
        None,

        // The row takes the drop inside it.
        Target,

        // The bar is in the space under the row.
        GapBelow,

        // The bar is at the top of the row.
        GapTop,
    }

    // The drop mark of a row. The mark sets the pseudo classes :droptarget, :dropgap and :droptop that the styles of the tree read.
    public static class TreeDropMark
    {
        private const string DropTarget = ":droptarget";
        private const string DropGap = ":dropgap";
        private const string DropTop = ":droptop";

        public static readonly AttachedProperty<DropMark> MarkProperty =
            AvaloniaProperty.RegisterAttached<TreeViewItem, DropMark>(
                "Mark",
                typeof(TreeDropMark),
                DropMark.None);

        public static void SetMark(TreeViewItem element, DropMark value) => element.SetValue(MarkProperty, value);
        public static DropMark GetMark(TreeViewItem element) => element.GetValue(MarkProperty);

        static TreeDropMark()
        {
            MarkProperty.Changed.AddClassHandler<TreeViewItem>(OnMarkChanged);
        }

        private static void OnMarkChanged(TreeViewItem element, AvaloniaPropertyChangedEventArgs e)
        {
            var mark = e.GetNewValue<DropMark>();
            var pseudoClasses = (IPseudoClasses)element.Classes;
            pseudoClasses.Set(DropTarget, mark == DropMark.Target);
            pseudoClasses.Set(DropGap, mark == DropMark.GapBelow);
            pseudoClasses.Set(DropTop, mark == DropMark.GapTop);
        }
    }
}
