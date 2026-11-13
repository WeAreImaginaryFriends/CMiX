// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;

namespace CMiX.UI.Controls.Parts
{
    // The surface of every popup of CMiX: the shadow, the room of the shadow, the fill and the corners.
    // PopupFrame, PopupCard and DropdownFrame show it. Each of them keeps its own placement.
    // Padding is the room around the content. CornerRadius is the corners of the fill.
    public class PopupSurface : ContentControl
    {
        // The room of the shadow around the fill.
        public static readonly StyledProperty<Thickness> ShadowMarginProperty =
            AvaloniaProperty.Register<PopupSurface, Thickness>(nameof(ShadowMargin), PopupFrame.ShadowMargin);

        public Thickness ShadowMargin
        {
            get => GetValue(ShadowMarginProperty);
            set => SetValue(ShadowMarginProperty, value);
        }

        // The smallest width of the fill.
        public static readonly StyledProperty<double> SurfaceMinWidthProperty =
            AvaloniaProperty.Register<PopupSurface, double>(nameof(SurfaceMinWidth));

        public double SurfaceMinWidth
        {
            get => GetValue(SurfaceMinWidthProperty);
            set => SetValue(SurfaceMinWidthProperty, value);
        }

        // A part that lies on the fill and has the shadow too: the tip of a card.
        public static readonly StyledProperty<object?> OverlayProperty =
            AvaloniaProperty.Register<PopupSurface, object?>(nameof(Overlay));

        public object? Overlay
        {
            get => GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }
    }
}
