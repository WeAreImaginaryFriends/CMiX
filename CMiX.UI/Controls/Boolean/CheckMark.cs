// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace CMiX.UI.Controls.Boolean
{
    // The check box of CMiX as a picture: a box with a check mark. It shows IsChecked and does not change it.
    // The toggle that owns it takes the click. Every check box of CMiX is this one control.
    public class CheckMark : TemplatedControl
    {
        public static readonly StyledProperty<bool?> IsCheckedProperty =
            AvaloniaProperty.Register<CheckMark, bool?>(nameof(IsChecked), false);

        public bool? IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsCheckedProperty)
                PseudoClasses.Set(":checked", IsChecked == true);
        }
    }
}
