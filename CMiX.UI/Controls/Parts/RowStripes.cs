// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Linq;
using CMiX.UI.Panels;

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
            AvaloniaProperty.Register<RowStripes, double>(nameof(AlternateOpacity), 0.8);

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

        public static readonly StyledProperty<string?> RowNameProperty =
            AvaloniaProperty.Register<RowStripes, string?>(nameof(RowName));

        // The name of the control that is the row, inside the Rows panel. With a name, a band starts at the top of each real row
        // and goes down to the top of the next one. The rows of a list are not one pitch apart on a screen with a scaling like 125%:
        // every panel rounds its size to whole pixels, so a long list of rows drifts away from a pattern of one pitch.
        // Without a name, the bands are a pattern of one pitch from the top of the Rows panel.
        public string? RowName
        {
            get => GetValue(RowNameProperty);
            set => SetValue(RowNameProperty, value);
        }

        private IReadOnlyList<(Rect Band, double Opacity)> lastBands = Array.Empty<(Rect, double)>();

        static RowStripes()
        {
            AffectsRender<RowStripes>(RowHeightProperty, RowSpacingProperty, BrushProperty, AlternateOpacityProperty, RowsProperty, RowNameProperty);
        }

        public RowStripes()
        {
            IsHitTestVisible = false;

            // The first and the last band are cut by the edges of the control. They must not be drawn over the neighbors.
            ClipToBounds = true;
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

        // The rows move when the list scrolls, opens or closes, or the panel grows: the bands move with them.
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            var bands = Bands();
            if (bands.SequenceEqual(lastBands))
                return;

            lastBands = bands;
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

            if (RealRowTops() is { Count: > 0 } tops)
                return BandsOfRows(tops, pitch);

            var origin = Origin();
            for (var index = (int)Math.Floor(-origin / pitch); origin + index * pitch < Bounds.Height; index++)
                bands.Add((new Rect(0, origin + index * pitch, Bounds.Width, pitch), OpacityOf(index)));

            return bands;
        }

        // The first row, the third row and so on have the color as it is. The other rows have AlternateOpacity.
        private double OpacityOf(int index) => ((index % 2) + 2) % 2 == 0 ? 1 : AlternateOpacity;

        // The tops of the rows that are shown, from the top, in this control. Null when no row name is set.
        private List<double>? RealRowTops()
        {
            if (string.IsNullOrEmpty(RowName) || Rows == null)
                return null;

            var tops = new List<double>();
            foreach (var visual in Rows.GetVisualDescendants())
            {
                if (visual is Control control && control.Name == RowName && control.IsEffectivelyVisible && !IsHiddenByAnimation(control)
                    && control.TranslatePoint(new Point(0, 0), this) is { } point)
                    tops.Add(point.Y);
            }

            tops.Sort();
            return tops;
        }

        // A row inside a panel that opens or closes can lie below the part of the panel that is shown. It is not a row to see.
        private bool IsHiddenByAnimation(Control row)
        {
            for (var panel = row.FindAncestorOfType<CollapsePanel>(); panel != null; panel = panel.FindAncestorOfType<CollapsePanel>())
            {
                if (ReferenceEquals(panel, Rows) || !Rows!.IsVisualAncestorOf(panel))
                    break;

                if (panel.Progress < 1 && row.TranslatePoint(new Point(0, 0), panel) is { } inPanel && inPanel.Y >= panel.Bounds.Height)
                    return true;
            }

            return false;
        }

        // A band starts at the top of each row and goes down to the top of the next row, so the spacing between two rows belongs
        // to the upper band. The last row has the nominal height. Above the first row and under the last row the bands go on with
        // the nominal pitch. Only the bands that touch the control are made, but they keep their place in the pattern.
        private IReadOnlyList<(Rect Band, double Opacity)> BandsOfRows(List<double> tops, double pitch)
        {
            var bands = new List<(Rect, double)>();
            void Add(double top, double bottom, int index)
            {
                if (bottom > 0 && top < Bounds.Height)
                    bands.Add((new Rect(0, top, Bounds.Width, bottom - top), OpacityOf(index)));
            }

            var above = new List<(Rect, double)>();
            var aboveTop = tops[0];
            for (var index = -1; aboveTop > 0; index--)
            {
                aboveTop -= pitch;
                above.Add((new Rect(0, aboveTop, Bounds.Width, pitch), OpacityOf(index)));
            }

            above.Reverse();
            bands.AddRange(above);

            for (var i = 0; i < tops.Count; i++)
                Add(tops[i], i + 1 < tops.Count ? tops[i + 1] : tops[i] + pitch, i);

            var bottom = tops[^1] + pitch;
            for (var index = tops.Count; bottom < Bounds.Height; index++)
            {
                bands.Add((new Rect(0, bottom, Bounds.Width, pitch), OpacityOf(index)));
                bottom += pitch;
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
