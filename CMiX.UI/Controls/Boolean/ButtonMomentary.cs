// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CMiX.UI.Controls.Parts;

namespace CMiX.UI.Controls.Boolean
{
    // A Button that holds a bool: true while the pointer holds it down, false on release.
    // A Value set from outside shows as held. It shows a Reset menu while it has a ResetCommand.
    public class ButtonMomentary : Button
    {
        public static readonly StyledProperty<bool> ValueProperty =
            AvaloniaProperty.Register<ButtonMomentary, bool>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);
        public bool Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<ICommand?> ResetCommandProperty =
            AvaloniaProperty.Register<ButtonMomentary, ICommand?>(nameof(ResetCommand));
        public ICommand? ResetCommand
        {
            get => GetValue(ResetCommandProperty);
            set => SetValue(ResetCommandProperty, value);
        }

        public ButtonMomentary()
        {
            ValueMenu.Follow(this, ResetCommandProperty);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsPressedProperty)
                Value = change.GetNewValue<bool>();

            if (change.Property == ValueProperty)
                PseudoClasses.Set(":held", change.GetNewValue<bool>());
        }
    }
}
