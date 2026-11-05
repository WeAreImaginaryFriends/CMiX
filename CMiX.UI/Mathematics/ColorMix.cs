// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia.Media;

namespace CMiX.UI.Mathematics
{
    public static class ColorMix
    {
        // The color between two colors. An amount of 0 gives the first color, and an amount of 1 gives the second.
        // The amount stays between 0 and 1. Each channel is rounded to the nearest value.
        public static Color Lerp(Color from, Color to, double amount)
        {
            var by = Math.Clamp(amount, 0, 1);
            return Color.FromArgb(
                Channel(from.A, to.A, by),
                Channel(from.R, to.R, by),
                Channel(from.G, to.G, by),
                Channel(from.B, to.B, by));
        }

        // A color seen over a background with a strength: 0 gives the background, 1 gives the color.
        public static Color Over(Color color, Color background, double strength) => Lerp(background, color, strength);

        private static byte Channel(byte from, byte to, double by) =>
            (byte)Math.Round(MathHelper.Lerp(from, to, by), MidpointRounding.AwayFromZero);
    }
}
