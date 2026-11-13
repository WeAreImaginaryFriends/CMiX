// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using CMiX.UI.Popups;

namespace CMiX.UI.Controls.Boolean
{
    // A Button whose content is one icon. IconKey names the icon. Tip is the tooltip. An empty Tip gives no tooltip.
    // TipShortcut and TipDescription add lines to the tooltip (see TipContent). DisabledReason is the line that explains why the button does
    // nothing: it shows in the tooltip while the button is disabled, and the tooltip shows on a disabled button then.
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

        public static readonly StyledProperty<string?> TipShortcutProperty =
            AvaloniaProperty.Register<ButtonIcon, string?>(nameof(TipShortcut));

        // The shortcut of the command of the button, such as "Shift+A".
        public string? TipShortcut
        {
            get => GetValue(TipShortcutProperty);
            set => SetValue(TipShortcutProperty, value);
        }

        public static readonly StyledProperty<string?> TipDescriptionProperty =
            AvaloniaProperty.Register<ButtonIcon, string?>(nameof(TipDescription));

        public string? TipDescription
        {
            get => GetValue(TipDescriptionProperty);
            set => SetValue(TipDescriptionProperty, value);
        }

        public static readonly StyledProperty<string?> DisabledReasonProperty =
            AvaloniaProperty.Register<ButtonIcon, string?>(nameof(DisabledReason));

        public string? DisabledReason
        {
            get => GetValue(DisabledReasonProperty);
            set => SetValue(DisabledReasonProperty, value);
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
            // The icon has the opacity of an action icon, from the token OpacityIconAction.
            var icon = new AppIcon();
            icon.Bind(OpacityProperty, icon.GetResourceObservable("OpacityIconAction"));
            icon.Bind(AppIcon.IconKeyProperty, this.GetObservable(IconKeyProperty));
            icon.Bind(AppIcon.SizeProperty, this.GetObservable(IconSizeProperty));
            Content = icon;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TipProperty || change.Property == TipShortcutProperty || change.Property == TipDescriptionProperty
                || change.Property == DisabledReasonProperty || change.Property == IsEffectivelyEnabledProperty)
                UpdateTip();
        }

        // A plain Tip is a string. With a shortcut, a description or a reason, the tip is a TipContent.
        private void UpdateTip()
        {
            if (string.IsNullOrEmpty(Tip))
            {
                ToolTip.SetTip(this, null);
                return;
            }

            var reason = IsEffectivelyEnabled || string.IsNullOrEmpty(DisabledReason) ? null : DisabledReason;
            ToolTip.SetTip(this, TipShortcut == null && TipDescription == null && reason == null ? Tip : new TipContent(Tip, TipShortcut, TipDescription, reason));
            ToolTip.SetShowOnDisabled(this, !string.IsNullOrEmpty(DisabledReason));
        }
    }
}
