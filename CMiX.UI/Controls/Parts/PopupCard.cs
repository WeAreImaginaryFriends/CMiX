// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using CMiX.UI.Popups;

namespace CMiX.UI.Controls.Parts
{
    // The content of a tooltip or of a panel: a dark card with a thin border, the room of the shadow around it, and an optional tip
    // that points at the anchor. The popup that holds it needs no frame of its own.
    // The popup placement (see PopupAnchoring) tells the card where its tip goes: call Place with the result.
    public class PopupCard : ContentControl
    {
        public static readonly StyledProperty<PopupSide> TipEdgeProperty =
            AvaloniaProperty.Register<PopupCard, PopupSide>(nameof(TipEdge), PopupSide.Top);

        // The edge of the card where the tip is. A tip on the top edge points up.
        public PopupSide TipEdge
        {
            get => GetValue(TipEdgeProperty);
            set => SetValue(TipEdgeProperty, value);
        }

        public static readonly StyledProperty<double> TipOffsetProperty =
            AvaloniaProperty.Register<PopupCard, double>(nameof(TipOffset));

        // The distance from the start of the edge (its left or its top end) to the middle of the tip.
        public double TipOffset
        {
            get => GetValue(TipOffsetProperty);
            set => SetValue(TipOffsetProperty, value);
        }

        public static readonly StyledProperty<bool> ShowsTipProperty =
            AvaloniaProperty.Register<PopupCard, bool>(nameof(ShowsTip));

        public bool ShowsTip
        {
            get => GetValue(ShowsTipProperty);
            set => SetValue(ShowsTipProperty, value);
        }

        private PopupTip? tip;

        protected override Type StyleKeyOverride => typeof(PopupCard);

        // Puts the tip where the placement says, and shows it.
        public void Place(PlacementResult result)
        {
            TipEdge = result.TipEdge;
            TipOffset = result.TipOffset;
            ShowsTip = true;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            tip = e.NameScope.Find<PopupTip>("PART_Tip");
            UpdateTip();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == TipEdgeProperty || change.Property == TipOffsetProperty)
                UpdateTip();
        }

        // The tip is a child of the card box. It sits on the outside of its edge, with its base on the border, and is moved along the
        // edge by its margin.
        private void UpdateTip()
        {
            if (tip == null)
                return;

            var along = TipOffset - PopupTip.Base / 2;
            var outside = -PopupTip.Depth;
            tip.Edge = TipEdge;
            switch (TipEdge)
            {
                case PopupSide.Top:
                    tip.HorizontalAlignment = HorizontalAlignment.Left;
                    tip.VerticalAlignment = VerticalAlignment.Top;
                    tip.Margin = new Thickness(along, outside, 0, 0);
                    break;
                case PopupSide.Bottom:
                    tip.HorizontalAlignment = HorizontalAlignment.Left;
                    tip.VerticalAlignment = VerticalAlignment.Bottom;
                    tip.Margin = new Thickness(along, 0, 0, outside);
                    break;
                case PopupSide.Left:
                    tip.HorizontalAlignment = HorizontalAlignment.Left;
                    tip.VerticalAlignment = VerticalAlignment.Top;
                    tip.Margin = new Thickness(outside, along, 0, 0);
                    break;
                default:
                    tip.HorizontalAlignment = HorizontalAlignment.Right;
                    tip.VerticalAlignment = VerticalAlignment.Top;
                    tip.Margin = new Thickness(0, along, outside, 0);
                    break;
            }
        }
    }
}
