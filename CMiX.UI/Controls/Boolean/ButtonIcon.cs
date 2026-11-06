// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;

namespace CMiX.UI.Controls.Boolean
{
    // A Button whose content is one icon. IconKey names the icon. Tip is the tooltip. An empty Tip gives no tooltip.
    // The theme sets IconSize to IconSizeXS.
    public class ButtonIcon : Button
    {
        public static readonly StyledProperty<string> IconKeyProperty =
            AvaloniaProperty.Register<ButtonIcon, string>(nameof(IconKey), string.Empty);

        public string IconKey
        {
            get => GetValue(IconKeyProperty);
            set => SetValue(IconKeyProperty, value);
        }

        public static readonly StyledProperty<string?> TipProperty =
            AvaloniaProperty.Register<ButtonIcon, string?>(nameof(Tip));

        public string? Tip
        {
            get => GetValue(TipProperty);
            set => SetValue(TipProperty, value);
        }

        public static readonly StyledProperty<double> IconSizeProperty =
            AvaloniaProperty.Register<ButtonIcon, double>(nameof(IconSize), 16);

        public double IconSize
        {
            get => GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public ButtonIcon()
        {
            var icon = new AppIcon { Opacity = 0.9 };
            icon.Bind(AppIcon.IconKeyProperty, this.GetObservable(IconKeyProperty));
            icon.Bind(AppIcon.SizeProperty, this.GetObservable(IconSizeProperty));
            Content = icon;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TipProperty)
            {
                var tip = change.GetNewValue<string?>();
                ToolTip.SetTip(this, string.IsNullOrEmpty(tip) ? null : tip);
            }
        }
    }
}
