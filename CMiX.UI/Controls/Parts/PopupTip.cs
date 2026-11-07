// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CMiX.UI.Popups;

namespace CMiX.UI.Controls.Parts
{
    // The small tip of a card (a tooltip or a panel) that points at the anchor. It sits on one edge of the card and can be on any of the four.
    // It is a triangle with a border on its two slanted sides. Its base goes one pixel into the card, so the tip breaks the border
    // of the card where it sits. The card places the tip (see PopupCard).
    public class PopupTip : Control
    {
        // The length of the base and the height of the tip, in pixels. The height must stay under the room of the shadow of a popup
        // (PopupFrame.ShadowMargin), because the tip is outside the card.
        public const double Base = 10;
        public const double Depth = 5;

        // How far the base goes into the card, to cover the border of the card.
        public const double Overlap = 1;

        public static readonly StyledProperty<PopupSide> EdgeProperty =
            AvaloniaProperty.Register<PopupTip, PopupSide>(nameof(Edge), PopupSide.Top);

        // The edge of the card where the tip is. A tip on the top edge points up.
        public PopupSide Edge
        {
            get => GetValue(EdgeProperty);
            set => SetValue(EdgeProperty, value);
        }

        public static readonly StyledProperty<IBrush?> FillProperty =
            AvaloniaProperty.Register<PopupTip, IBrush?>(nameof(Fill));

        public IBrush? Fill
        {
            get => GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly StyledProperty<IBrush?> StrokeProperty =
            AvaloniaProperty.Register<PopupTip, IBrush?>(nameof(Stroke));

        public IBrush? Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        static PopupTip()
        {
            AffectsMeasure<PopupTip>(EdgeProperty);
            AffectsRender<PopupTip>(EdgeProperty, FillProperty, StrokeProperty);
        }

        public PopupTip()
        {
            IsHitTestVisible = false;
        }

        // The size of the tip for an edge: wide on the top and the bottom edge, high on the left and the right edge.
        public static Size SizeOf(PopupSide edge) =>
            edge is PopupSide.Top or PopupSide.Bottom
                ? new Size(Base, Depth + Overlap)
                : new Size(Depth + Overlap, Base);

        // The corners of the tip in its own box: the end of the base on the card, the point, then the other end of the base.
        // The first and the last point are on the line of the border of the card.
        internal static Point[] Outline(PopupSide edge)
        {
            var size = SizeOf(edge);
            var middle = Base / 2;
            return edge switch
            {
                PopupSide.Top => new[] { new Point(0, Depth), new Point(middle, 0), new Point(Base, Depth) },
                PopupSide.Bottom => new[] { new Point(0, Overlap), new Point(middle, Depth + Overlap), new Point(Base, Overlap) },
                PopupSide.Left => new[] { new Point(Depth, 0), new Point(0, middle), new Point(Depth, Base) },
                _ => new[] { new Point(Overlap, 0), new Point(Depth + Overlap, middle), new Point(Overlap, Base) },
            };
        }

        protected override Size MeasureOverride(Size availableSize) => SizeOf(Edge);

        public override void Render(DrawingContext context)
        {
            var edge = Edge;
            var size = SizeOf(edge);
            var points = Outline(edge);

            // The fill covers the border of the card under the base of the tip.
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var cover = edge switch
                {
                    PopupSide.Top => new Point(0, size.Height),
                    PopupSide.Bottom => new Point(0, 0),
                    PopupSide.Left => new Point(size.Width, 0),
                    _ => new Point(0, 0),
                };
                var coverEnd = edge switch
                {
                    PopupSide.Top => new Point(Base, size.Height),
                    PopupSide.Bottom => new Point(Base, 0),
                    PopupSide.Left => new Point(size.Width, Base),
                    _ => new Point(0, Base),
                };
                ctx.BeginFigure(cover, true);
                ctx.LineTo(points[0]);
                ctx.LineTo(points[1]);
                ctx.LineTo(points[2]);
                ctx.LineTo(coverEnd);
                ctx.EndFigure(true);
            }

            context.DrawGeometry(Fill, null, geometry);

            if (Stroke is { } stroke)
            {
                var pen = new Pen(stroke, 1);
                context.DrawLine(pen, points[0], points[1]);
                context.DrawLine(pen, points[1], points[2]);
            }
        }
    }
}
