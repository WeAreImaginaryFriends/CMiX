// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;

namespace CMiX.UI.Popups
{
    // The side of the anchor where a popup opens.
    public enum PopupSide
    {
        Top,
        Bottom,
        Left,
        Right,
    }

    // What the rule needs to place a card. Every value is in the same coordinates, in device independent pixels.
    // Anchor is what the card stays away from: a control or a row. Card is the size of the visible card, without the room of the shadow.
    // Bounds is the room that the card may use, such as the working area of a screen. Gap is the space between the anchor and the card.
    // Margin is the least space between the card and the edge of the bounds.
    // Along is the point on the anchor that the card and its tip follow: for a bottom or top card it is an x, for a left or right card
    // it is a y. When it is null, the middle of the anchor is used (the pointer gives a value for a tooltip).
    // AlignOffset is the distance from the start of the card to the point of the card that lines up with Along. When it is null,
    // the middle of the card lines up. TipInset is the least distance between the tip and a corner of the card.
    public readonly record struct PlacementRequest(
        Rect Anchor,
        Size Card,
        Rect Bounds,
        PopupSide Side,
        double Gap = 8,
        double Margin = 8,
        double? Along = null,
        double? AlignOffset = null,
        double TipInset = 6);

    // Where the card goes. Position is the top left corner of the card. Side is the side of the anchor where the card is, after a flip.
    // TipOffset is the distance from the start of the tip edge (the left or the top of the card) to the tip.
    public readonly record struct PlacementResult(Point Position, PopupSide Side, double TipOffset, bool Flipped)
    {
        // The edge of the card where the tip is. It faces the anchor.
        public PopupSide TipEdge => Side switch
        {
            PopupSide.Bottom => PopupSide.Top,
            PopupSide.Top => PopupSide.Bottom,
            PopupSide.Right => PopupSide.Left,
            _ => PopupSide.Right,
        };
    }

    // The one rule that places a card next to an anchor, for the tooltips and the panels. It has no UI in it, so a test can check numbers.
    // The card goes on the wanted side, Gap away from the anchor. When it does not fit there it goes on the opposite side. When it fits on
    // neither side it goes on the side with more room. Along the side, the card follows the point Along and stops at the margin.
    // The tip keeps pointing at Along, inside the inset of the corners.
    public static class PopupPlacement
    {
        public static PlacementResult Place(PlacementRequest request)
        {
            var side = request.Side;
            var flipped = false;
            if (Room(request, side) < CrossSize(request, side))
            {
                var opposite = Opposite(side);
                if (Room(request, opposite) >= CrossSize(request, opposite) || Room(request, opposite) > Room(request, side))
                {
                    side = opposite;
                    flipped = true;
                }
            }

            var vertical = side is PopupSide.Top or PopupSide.Bottom;
            var anchor = request.Anchor;
            var card = request.Card;
            var bounds = request.Bounds;

            var cross = side switch
            {
                PopupSide.Bottom => anchor.Bottom + request.Gap,
                PopupSide.Top => anchor.Top - request.Gap - card.Height,
                PopupSide.Right => anchor.Right + request.Gap,
                _ => anchor.Left - request.Gap - card.Width,
            };
            cross = vertical
                ? Limit(cross, bounds.Top + request.Margin, bounds.Bottom - request.Margin - card.Height)
                : Limit(cross, bounds.Left + request.Margin, bounds.Right - request.Margin - card.Width);

            var length = vertical ? card.Width : card.Height;
            var target = request.Along ?? (vertical ? anchor.Center.X : anchor.Center.Y);
            var start = target - (request.AlignOffset ?? length / 2);
            start = vertical
                ? Limit(start, bounds.Left + request.Margin, bounds.Right - request.Margin - length)
                : Limit(start, bounds.Top + request.Margin, bounds.Bottom - request.Margin - length);

            var inset = Math.Min(request.TipInset, length / 2);
            var tip = Limit(target - start, inset, length - inset);

            var position = vertical ? new Point(start, cross) : new Point(cross, start);
            return new PlacementResult(position, side, tip, flipped);
        }

        // The space between the anchor and the margin of the bounds, on one side, after the gap.
        private static double Room(PlacementRequest request, PopupSide side) => side switch
        {
            PopupSide.Bottom => request.Bounds.Bottom - request.Margin - (request.Anchor.Bottom + request.Gap),
            PopupSide.Top => request.Anchor.Top - request.Gap - (request.Bounds.Top + request.Margin),
            PopupSide.Right => request.Bounds.Right - request.Margin - (request.Anchor.Right + request.Gap),
            _ => request.Anchor.Left - request.Gap - (request.Bounds.Left + request.Margin),
        };

        // The size of the card across the anchor: its height above or below, its width at the side.
        private static double CrossSize(PlacementRequest request, PopupSide side) =>
            side is PopupSide.Top or PopupSide.Bottom ? request.Card.Height : request.Card.Width;

        private static PopupSide Opposite(PopupSide side) => side switch
        {
            PopupSide.Top => PopupSide.Bottom,
            PopupSide.Bottom => PopupSide.Top,
            PopupSide.Left => PopupSide.Right,
            _ => PopupSide.Left,
        };

        // A value between the least and the greatest. When the room is too small, the least wins.
        private static double Limit(double value, double least, double greatest) =>
            greatest < least ? least : Math.Min(Math.Max(value, least), greatest);
    }
}
