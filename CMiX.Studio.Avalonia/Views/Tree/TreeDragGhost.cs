// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace CMiX.Studio.Avalonia.Views
{
    // A copy of the dragged row that follows the pointer, so the user sees what the drag holds. Avalonia has no drag image for a
    // drag with the pointer, so the copy is a brush of the row, on the overlay layer of the window above all the other content.
    // The copy takes no pointer events. It keeps the point of the row that the user grabbed under the pointer.
    internal sealed class TreeDragGhost
    {
        // The copy is a little transparent, so the rows under it stay readable.
        public const double GhostOpacity = 0.8;

        private readonly Border ghost = new() { IsHitTestVisible = false, Opacity = GhostOpacity };
        private OverlayLayer? layer;
        private Point grab;

        // True while the copy is on the overlay layer.
        public bool IsShown => layer != null;

        // The copy that is shown. A test reads it.
        internal Border Ghost => ghost;

        // Shows the copy of the row. The position is the pointer, in the coordinates of the reference control.
        public void Show(Control row, Control reference, Point position)
        {
            Hide();
            if (OverlayLayer.GetOverlayLayer(reference) is not { } overlay || row.Bounds.Width <= 0 || row.Bounds.Height <= 0)
                return;

            ghost.Width = row.Bounds.Width;
            ghost.Height = row.Bounds.Height;
            ghost.Background = new VisualBrush(row) { Stretch = Stretch.Fill };
            grab = reference.TranslatePoint(position, row) is { } inRow
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
        }

        private static double Clamp(double value, double max) => value < 0 ? 0 : value > max ? max : value;
    }
}
