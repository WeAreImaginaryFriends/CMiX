// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.VisualTree;
using CMiX.UI.Controls.Parts;

namespace CMiX.UI.Popups
{
    // Puts a tooltip, a popup or a context menu where PopupPlacement says. The popup asks for its place with a callback of Avalonia.
    // The callback gets the size of the popup and the control it belongs to. This class turns them into a PlacementRequest, runs the rule,
    // and gives the corner of the card back to Avalonia. Avalonia does no flip and no slide itself.
    // A popup has the room of a shadow around its card (see PopupFrame), so the card goes Shadow further in than the popup.
    // The coordinates of the rule are the coordinates of the window that holds the control, in device independent pixels.
    // Avalonia calls the callback again when an offset of the tooltip or of the popup changes. A tooltip that follows the pointer
    // changes Along, then changes its horizontal offset by a small step.
    public sealed class PopupAnchoring
    {
        // The side of the anchor where the card goes, before a flip.
        public PopupSide Side { get; set; } = PopupSide.Bottom;

        public double Gap { get; set; } = 8;

        public double Margin { get; set; } = 8;

        // The point on the anchor that the card and its tip follow, in the coordinates of the window. See PlacementRequest.Along.
        public double? Along { get; set; }

        // The distance from the start of the card to the point that lines up with Along. See PlacementRequest.AlignOffset.
        public double? AlignOffset { get; set; }

        // The room around the card inside the popup. A tooltip and a panel use the shadow room of the popup frame.
        public Thickness Shadow { get; set; } = PopupFrame.ShadowMargin;

        // The anchor, in the coordinates of the window. When it is null, the anchor is the control that the popup belongs to.
        public Rect? Anchor { get; set; }

        // The room that the card may use, in the coordinates of the window. When it is null, it is the working area of the screen
        // of the control.
        public Rect? Bounds { get; set; }

        // The last result. It is null before the popup is placed.
        public PlacementResult? Last { get; private set; }

        // Raised each time the popup is placed, so the card can move its tip.
        public event Action<PlacementResult>? Placed;

        // Makes a tooltip of a control use this rule.
        public static PopupAnchoring Use(Control control, PopupAnchoring? anchoring = null)
        {
            anchoring ??= new PopupAnchoring();
            ToolTip.SetPlacement(control, PlacementMode.Custom);
            ToolTip.SetCustomPopupPlacementCallback(control, anchoring.Place);
            return anchoring;
        }

        // Makes a popup use this rule.
        public static PopupAnchoring Use(Popup popup, PopupAnchoring? anchoring = null)
        {
            anchoring ??= new PopupAnchoring();
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = anchoring.Place;
            return anchoring;
        }

        // Makes a flyout use this rule.
        public static PopupAnchoring Use(PopupFlyoutBase flyout, PopupAnchoring? anchoring = null)
        {
            anchoring ??= new PopupAnchoring();
            flyout.Placement = PlacementMode.Custom;
            flyout.CustomPopupPlacementCallback = anchoring.Place;
            return anchoring;
        }

        // Makes a context menu use this rule.
        public static PopupAnchoring Use(ContextMenu menu, PopupAnchoring? anchoring = null)
        {
            anchoring ??= new PopupAnchoring();
            menu.Placement = PlacementMode.Custom;
            menu.CustomPopupPlacementCallback = anchoring.Place;
            return anchoring;
        }

        // The rectangle of a control in the coordinates of its window. It is empty when the control is not in a window.
        public static Rect RectOf(Visual visual)
        {
            var top = TopLevel.GetTopLevel(visual);
            var origin = top == null ? null : visual.TranslatePoint(default, top);
            return origin is { } point ? new Rect(point, visual.Bounds.Size) : default;
        }

        // The working area of the screen of a control, in the coordinates of its window. A screen that is not the one of the window
        // can have negative coordinates. Without a screen, it is the client area of the window.
        public static Rect WorkingAreaOf(Visual visual)
        {
            var top = TopLevel.GetTopLevel(visual);
            if (top == null)
                return default;

            if (top.Screens?.ScreenFromVisual(visual) is { } screen)
            {
                var area = screen.WorkingArea;
                var from = top.PointToClient(area.TopLeft);
                var to = top.PointToClient(area.BottomRight);
                return new Rect(from, to);
            }

            return new Rect(top.ClientSize);
        }

        private void Place(CustomPopupPlacement placement)
        {
            if (placement.Target is not Visual target)
                return;

            var anchor = Anchor ?? RectOf(target);
            var bounds = Bounds ?? WorkingAreaOf(target);
            var card = new Size(
                Math.Max(0, placement.PopupSize.Width - Shadow.Left - Shadow.Right),
                Math.Max(0, placement.PopupSize.Height - Shadow.Top - Shadow.Bottom));

            var result = PopupPlacement.Place(new PlacementRequest(anchor, card, bounds, Side, Gap, Margin, Along, AlignOffset));

            // The popup starts Shadow before the card. The rectangle is one point, and the popup hangs from it to the bottom right.
            var corner = new Point(result.Position.X - Shadow.Left, result.Position.Y - Shadow.Top);
            placement.AnchorRectangle = new Rect(corner, new Size(1, 1));
            placement.Anchor = PopupAnchor.TopLeft;
            placement.Gravity = PopupGravity.BottomRight;
            placement.Offset = default;
            placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;

            Last = result;
            Placed?.Invoke(result);
        }
    }
}
