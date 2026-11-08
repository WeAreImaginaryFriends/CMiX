// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace CMiX.Studio.Avalonia.Views
{
    // A copy of the dragged row that follows the pointer, so the user sees what the drag holds. Avalonia has no drag image for a
    // drag with the pointer, so the copy is a picture of the row, on the overlay layer of the window above all the other content.
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

        // Shows the copy of the row. The grab is the point where the button was pressed and the position is the pointer now, both in the
        // coordinates of the reference control. The copy keeps the grabbed point of the row under the pointer, from the first move on.
        public void Show(Control row, Control reference, Point grabbed, Point position)
        {
            Hide();
            if (OverlayLayer.GetOverlayLayer(reference) is not { } overlay || row.Bounds.Width <= 0 || row.Bounds.Height <= 0)
                return;

            ghost.Width = row.Bounds.Width;
            ghost.Height = row.Bounds.Height;
            Paint(row, reference);
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

        // The copy is a picture of the whole row: its fill, its icon and its name. A brush of the row draws only the fill of the row, so
        // the row is drawn into a bitmap. Render draws a row at its own place in its parent, so the bitmap reaches from the corner of the
        // parent to the end of the row, and the copy is the part of it where the row is (the left and the top of the bitmap stay empty).
        // The bitmap is in device independent pixels at 96 dpi: a bigger bitmap with a higher dpi can scale the row twice.
        // When the bitmap cannot be made, the copy is a brush of the row.
        private void Paint(Control row, Control reference)
        {
            try
            {
                var x = (int)Math.Floor(row.Bounds.X);
                var y = (int)Math.Floor(row.Bounds.Y);
                var width = Math.Max(1, (int)Math.Ceiling(row.Bounds.Right) - x);
                var height = Math.Max(1, (int)Math.Ceiling(row.Bounds.Bottom) - y);
                var bitmap = new RenderTargetBitmap(new PixelSize(x + width, y + height), new Vector(96, 96));
                bitmap.Render(row);
                ghost.Background = null;
                // The whole bitmap is shown, moved up and to the left, and the copy cuts it to the size of the row. A CroppedBitmap
                // is not used: the picture of it is not the same as the bitmap on every renderer.
                ghost.ClipToBounds = true;
                ghost.Child = new global::Avalonia.Controls.Image
                {
                    Source = bitmap,
                    Width = x + width,
                    Height = y + height,
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
                    Margin = new Thickness(-x, -y, 0, 0),
                    IsHitTestVisible = false
                };
            }
            catch (Exception)
            {
                ghost.Child = null;
                ghost.Background = new VisualBrush(row) { Stretch = Stretch.Fill };
            }
        }
        private static double Clamp(double value, double max) => value < 0 ? 0 : value > max ? max : value;
    }
}
