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
            value is KeyGesture gesture ? TextOf(gesture) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        // The text that the user sees. A number key shows as its digit: Ctrl+D1 shows as "Ctrl+1".
        public static string TextOf(KeyGesture gesture)
        {
            var text = gesture.ToString();
            if (gesture.Key is < Key.D0 or > Key.D9)
                return text;

            var digit = (char)('0' + (gesture.Key - Key.D0));
            var name = "D" + digit;
            return text.EndsWith(name, StringComparison.Ordinal) ? text[..^name.Length] + digit : text;
        }

        // Reads a gesture text. A single digit key reads as the number key: "Ctrl+1" gives Ctrl+D1.
        // KeyGesture.Parse alone reads "1" as the key value 1. All other text goes to KeyGesture.Parse unchanged.
        public static KeyGesture Parse(string text)
        {
            var plus = text.LastIndexOf('+');
            var key = text[(plus + 1)..].Trim();
            if (key.Length == 1 && key[0] is >= '0' and <= '9')
                text = text[..(plus + 1)] + "D" + key;

            return KeyGesture.Parse(text);
        }
    }
}
