// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using CMiX.UI.Controls;

namespace CMiX.Studio.Avalonia.Views
{
    // A label of the dragged row that follows the pointer, so the user sees what the drag holds, as in Blender. Avalonia has no drag image
    // for a drag with the pointer, so the label is a control on the overlay layer of the window, above all the other content.
    // The label has the icon and the name of the node, on the fill of the row, and it is only as wide as its content. It is above the
    // pointer, with the middle of the icon right over the pointer, so the row under the pointer and the line of the drop stay in sight.
    // Near the top of the window there is no room above the pointer, so the label goes under it. The label takes no pointer events.
    internal sealed class TreeDragGhost
    {
        // The label is a little transparent, so the rows under it stay readable.
        public const double GhostOpacity = 0.8;

        private readonly Border ghost = new() { IsHitTestVisible = false, Opacity = GhostOpacity, ClipToBounds = true };
        private OverlayLayer? layer;

        // The distance from the left edge of the label to the middle of its icon.
        private double iconMiddle;

        // The space between the pointer and the label: the token SpacingSM.
        private double gap = 4;

        // True while the label is on the overlay layer.
        public bool IsShown => layer != null;

        // The label that is shown. A test reads it.
        internal Border Ghost => ghost;

        // Shows the label of the row, with the icon and the name of its node, at the pointer. The position is the pointer, in the
        // coordinates of the reference control.
        public void Show(Border row, string iconKey, string name, Control reference, Point position)
        {
            Hide();
            if (OverlayLayer.GetOverlayLayer(reference) is not { } overlay || row.Bounds.Width <= 0 || row.Bounds.Height <= 0)
                return;

            gap = Resource(row, "SpacingSM", 4.0);
            Build(row, iconKey, name);
            ghost.Height = row.Bounds.Height;
            ghost.Measure(Size.Infinity);

            layer = overlay;
            overlay.Children.Add(ghost);
            Move(reference, position, null);
        }

        // Moves the label with the pointer. The line top is the top of the line of a drop between rows, in the coordinates of the
        // reference control, or null. A line just above the pointer, for a drop before the row under it, would be under the label: the
        // label then goes above the line.
        public void Move(Control reference, Point position, double? lineTop = null)
        {
            if (layer == null || reference.TranslatePoint(position, layer) is not { } point)
                return;

            var width = ghost.DesiredSize.Width;
            var height = ghost.Height;
            var maxLeft = Math.Max(0, layer.Bounds.Width - width);
            var left = Math.Clamp(point.X - iconMiddle, 0, maxLeft);
            var bottom = point.Y - gap;
            if (lineTop is { } y && reference.TranslatePoint(new Point(0, y), layer) is { } line && line.Y < point.Y)
                bottom = Math.Min(bottom, line.Y - gap);

            var top = bottom - height;
            if (top < 0)
                top = point.Y + gap;

            Canvas.SetLeft(ghost, left);
            Canvas.SetTop(ghost, top);
        }

        // Takes the label away.
        public void Hide()
        {
            layer?.Children.Remove(ghost);
            layer = null;
            ghost.Background = null;
            ghost.Child = null;
        }

        // The label has the fill and the corners of the row as it is now, then the icon and the name of the node, with the margins of
        // the row (see ProjectTreeView.axaml). The space after the name is the space before the icon, so the label is even at both ends.
        private void Build(Border row, string iconKey, string name)
        {
            ghost.Background = row.Background;
            ghost.CornerRadius = row.CornerRadius;

            var headerMargin = Resource(row, "TreeHeaderMargin", new Thickness(4, 0));
            var contentMargin = Resource(row, "TreeRowContentMargin", default(Thickness));
            var iconSize = Resource(row, "IconSizeXS", 12.0);
            var foreground = TextElement.GetForeground(row);
            ghost.Padding = new Thickness(contentMargin.Left, 0, contentMargin.Left, 0);
            iconMiddle = ghost.Padding.Left + headerMargin.Left + iconSize / 2;

            var content = new StackPanel { Orientation = Orientation.Horizontal, IsHitTestVisible = false };
            content.Children.Add(new AppIcon
            {
                IconKey = iconKey,
                Size = iconSize,
                Width = iconSize,
                Margin = headerMargin,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = foreground
            });
            content.Children.Add(new TextBlock
            {
                Text = name,
                Margin = headerMargin,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = foreground
            });

            ghost.Child = content;
        }

        private static T Resource<T>(Control control, string key, T fallback) =>
            control.TryFindResource(key, out var value) && value is T typed ? typed : fallback;
    }
}
