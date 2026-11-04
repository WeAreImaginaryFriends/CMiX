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
    // They are one color: the bands in between have the same color with a lower opacity.
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

        public static readonly StyledProperty<IBrush?> BrushProperty =
            AvaloniaProperty.Register<RowStripes, IBrush?>(nameof(Brush));

        // The one color of the bands. The band of the first row, the third row, and so on has it as it is.
        // The other bands have the same color with AlternateOpacity. The control has no color of its own to manage.
        public IBrush? Brush
        {
            get => GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public static readonly StyledProperty<double> AlternateOpacityProperty =
            AvaloniaProperty.Register<RowStripes, double>(nameof(AlternateOpacity), 0.5);

        // The opacity of the band of the second row, the fourth row, and so on.
        public double AlternateOpacity
        {
            get => GetValue(AlternateOpacityProperty);
            set => SetValue(AlternateOpacityProperty, value);
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
            AffectsRender<RowStripes>(RowHeightProperty, RowSpacingProperty, BrushProperty, AlternateOpacityProperty, RowsProperty);
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

        // The bands that fill the control, from the top, and the opacity of the color on each.
        public IReadOnlyList<(Rect Band, double Opacity)> Bands()
        {
            var bands = new List<(Rect, double)>();
            var pitch = RowHeight + RowSpacing;
            if (pitch <= 0 || Bounds.Height <= 0)
                return bands;

            var origin = Origin();
            for (var index = (int)Math.Floor(-origin / pitch); origin + index * pitch < Bounds.Height; index++)
            {
                var first = ((index % 2) + 2) % 2 == 0;
                bands.Add((new Rect(0, origin + index * pitch, Bounds.Width, pitch), first ? 1 : AlternateOpacity));
            }

            return bands;
        }

        public override void Render(DrawingContext context)
        {
            if (Brush is not { } brush)
                return;

            foreach (var (band, opacity) in Bands())
                using (context.PushOpacity(opacity))
                    context.FillRectangle(brush, band);
        }
    }
}
