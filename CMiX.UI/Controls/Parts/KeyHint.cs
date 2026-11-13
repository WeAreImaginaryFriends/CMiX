// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using CMiX.UI.Converters;

namespace CMiX.UI.Controls.Parts
{
    // One hint of a KeyHintStrip: the texts of one or more keys, and the words that say what they do. For example "Shift+A" and "add".
    public sealed record KeyHint(IReadOnlyList<string> Keys, string Words)
    {
        public KeyHint(string key, string words) : this(new[] { key }, words)
        {
        }

        // A hint from gestures, for example from a shortcut map. A missing gesture gives no key. With no gesture at all, it gives null.
        public static KeyHint? Of(string words, params KeyGesture?[] gestures)
        {
            var keys = (gestures ?? Array.Empty<KeyGesture?>()).OfType<KeyGesture>().Select(KeyGestureTextConverter.TextOf).ToArray();
            return keys.Length == 0 ? null : new KeyHint(keys, words);
        }
    }
}
