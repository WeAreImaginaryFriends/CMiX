// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace CMiX.UI.Converters
{
    // The text of a key gesture, such as "Shift+A", for the shortcut line of a tooltip. A missing gesture gives no text.
    public sealed class KeyGestureTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is KeyGesture gesture ? gesture.ToString() : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
