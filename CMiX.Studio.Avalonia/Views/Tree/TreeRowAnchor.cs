// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CMiX.UI.Controls.String;

namespace CMiX.Studio.Avalonia.Views
{
    // Where a popup opens on a row when a key asks for it: the Add panel at the middle of the row, the menu under the row at the name.
    internal static class TreeRowAnchor
    {
        // The middle of the row border of the item, in the coordinates of the visual. Null when the row has no border in that visual.
        public static Point? CenterOf(TreeViewItem item, Visual relativeTo) =>
            TreeRows.RowBorderOf(item) is { } row ? row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), relativeTo) : null;

        // The part of the row border from the left edge of the name to the right edge of the row, in the coordinates of the visual. A popup
        // placed under it starts at the name. A row with no name box gives the whole row.
        public static Rect? FromNameOf(TreeViewItem item, Visual relativeTo)
        {
            if (TreeRows.RowBorderOf(item) is not { } row || row.TranslatePoint(default, relativeTo) is not { } topLeft)
                return null;

            var rect = new Rect(topLeft, row.Bounds.Size);
            var name = row.GetVisualDescendants().OfType<TextBoxEditable>().FirstOrDefault();
            return name?.TranslatePoint(default, relativeTo) is { } left ? rect.WithX(left.X).WithWidth(rect.Right - left.X) : rect;
        }
    }
}
