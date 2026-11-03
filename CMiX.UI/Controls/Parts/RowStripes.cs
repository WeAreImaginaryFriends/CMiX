// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace CMiX.UI.Controls.Parts
{
    // Two bands that alternate behind the rows of a list or a tree, to follow a row across the panel.
    // A band is one row high plus the space between two rows. The bands are a pattern behind the rows:
    // they do not depend on which rows are shown, so opening or closing a row does not change them.
    // The first band starts at the top of the Rows panel, and so it moves with the rows when they scroll.
    // Above the first row, and under the last row, the bands go on.
    // Every row must be one pitch from the next. Put the control behind the list, in the same cell.
    public class RowStripes : Control
    {
        public static readonly StyledProperty<double> RowHeightProperty =
            AvaloniaProperty.Register<RowStripes, double>(nameof(RowHeight));

        public double RowHeight
        {
            get => GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        public static readonly StyledProperty<double> RowSpacingProperty =
            AvaloniaProperty.Register<RowStripes, double>(nameof(RowSpacing));

        public double RowSpacing
        {
            get => GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        public static readonly StyledProperty<IBrush?> FirstBrushProperty =
            AvaloniaProperty.Register<RowStripes, IBrush?>(nameof(FirstBrush));

        // The brush of the band of the first row, the third row, and so on.
        public IBrush? FirstBrush
        {
            get => GetValue(FirstBrushProperty);
            set => SetValue(FirstBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush?> SecondBrushProperty =
            AvaloniaProperty.Register<RowStripes, IBrush?>(nameof(SecondBrush));

        // The brush of the band of the second row, the fourth row, and so on.
        public IBrush? SecondBrush
        {
            get => GetValue(SecondBrushProperty);
            set => SetValue(SecondBrushProperty, value);
        }

        public static readonly StyledProperty<Control?> RowsProperty =
            AvaloniaProperty.Register<RowStripes, Control?>(nameof(Rows));

        // The panel of the rows. Its top is where the first band starts.
        public Control? Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        private double lastOrigin = double.NaN;

        static RowStripes()
        {
            AffectsRender<RowStripes>(RowHeightProperty, RowSpacingProperty, FirstBrushProperty, SecondBrushProperty, RowsProperty);
        }

        public RowStripes()
        {
            IsHitTestVisible = false;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            LayoutUpdated += OnLayoutUpdated;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            LayoutUpdated -= OnLayoutUpdated;
            base.OnDetachedFromVisualTree(e);
        }

        // The rows move when the list scrolls or the panel grows: the bands move with them.
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            var origin = Origin();
            if (origin.Equals(lastOrigin))
                return;

            lastOrigin = origin;
            InvalidateVisual();
        }

        // The top of the rows in this control.
        private double Origin() => Rows?.TranslatePoint(new Point(0, 0), this)?.Y ?? 0;

        // The bands that fill the control, from the top, and the brush of each.
        public IReadOnlyList<(Rect Band, IBrush? Brush)> Bands()
        {
            var bands = new List<(Rect, IBrush?)>();
            var pitch = RowHeight + RowSpacing;
            if (pitch <= 0 || Bounds.Height <= 0)
                return bands;

            var origin = Origin();
            for (var index = (int)Math.Floor(-origin / pitch); origin + index * pitch < Bounds.Height; index++)
            {
                var first = ((index % 2) + 2) % 2 == 0;
                bands.Add((new Rect(0, origin + index * pitch, Bounds.Width, pitch), first ? FirstBrush : SecondBrush));
            }

            return bands;
        }

        public override void Render(DrawingContext context)
        {
            foreach (var (band, brush) in Bands())
                if (brush != null)
                    context.FillRectangle(brush, band);
        }
    }
}
