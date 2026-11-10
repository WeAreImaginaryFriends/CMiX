// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using CMiX.UI.Controls;

namespace CMiX.Studio.Avalonia.Views
{
    // A copy of the dragged row that follows the pointer, so the user sees what the drag holds. Avalonia has no drag image for a
    // drag with the pointer, so the copy is a control on the overlay layer of the window, above all the other content.
    // The copy takes no pointer events. It keeps the point of the row that the user grabbed under the pointer.
    internal sealed class TreeDragGhost
    {
        // The copy is a little transparent, so the rows under it stay readable.
        public const double GhostOpacity = 0.8;

        private readonly Border ghost = new() { IsHitTestVisible = false, Opacity = GhostOpacity, ClipToBounds = true };
        private OverlayLayer? layer;
        private Point grab;

        // True while the copy is on the overlay layer.
        public bool IsShown => layer != null;

        // The copy that is shown. A test reads it.
        internal Border Ghost => ghost;

        // Shows the copy of the row, with the icon and the name of its node. The grab is the point where the button was pressed and
        // the position is the pointer now, both in the coordinates of the reference control. The copy keeps the grabbed point of the
        // row under the pointer, from the first move on.
        public void Show(Border row, string iconKey, string name, Control reference, Point grabbed, Point position)
        {
            Hide();
            if (OverlayLayer.GetOverlayLayer(reference) is not { } overlay || row.Bounds.Width <= 0 || row.Bounds.Height <= 0)
                return;

            ghost.Width = row.Bounds.Width;
            ghost.Height = row.Bounds.Height;
            Build(row, iconKey, name);
            grab = reference.TranslatePoint(grabbed, row) is { } inRow
                ? new Point(Clamp(inRow.X, row.Bounds.Width), Clamp(inRow.Y, row.Bounds.Height))
                : default;

            layer = overlay;
            overlay.Children.Add(ghost);
            Move(reference, position);
        }

        // Moves the copy with the pointer.
        public void Move(Control reference, Point position)
        {
            if (layer == null || reference.TranslatePoint(position, layer) is not { } point)
                return;

            Canvas.SetLeft(ghost, point.X - grab.X);
            Canvas.SetTop(ghost, point.Y - grab.Y);
        }

        // Takes the copy away.
        public void Hide()
        {
            layer?.Children.Remove(ghost);
            layer = null;
            ghost.Background = null;
            ghost.Child = null;
        }

        // The copy has the fill and the corners of the row as it is now, then the icon and the name of the node, with the margins of
        // the row (see ProjectTreeView.axaml). It is made of controls, not of a picture of the row: a picture of the row sometimes
        // came without the icon and the name.
        private void Build(Border row, string iconKey, string name)
        {
            ghost.Background = row.Background;
            ghost.CornerRadius = row.CornerRadius;

            var headerMargin = Resource(row, "TreeHeaderMargin", new Thickness(4, 0));
            var foreground = TextElement.GetForeground(row);
            var content = new DockPanel { Margin = Resource(row, "TreeRowContentMargin", default(Thickness)), IsHitTestVisible = false };

            var icon = new AppIcon
            {
                IconKey = iconKey,
                Size = Resource(row, "IconSizeXS", 12.0),
                Margin = headerMargin,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = foreground
            };
            DockPanel.SetDock(icon, Dock.Left);
            content.Children.Add(icon);
            content.Children.Add(new TextBlock
            {
                Text = name,
                Margin = headerMargin,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = foreground,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            ghost.Child = content;
        }

        private static T Resource<T>(Control control, string key, T fallback) =>
            control.TryFindResource(key, out var value) && value is T typed ? typed : fallback;
        private static double Clamp(double value, double max) => value < 0 ? 0 : value > max ? max : value;
    }
}
