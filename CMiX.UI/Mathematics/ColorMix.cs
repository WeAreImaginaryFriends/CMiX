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

        // The relative luminance of a color, from 0 (black) to 1 (white), as WCAG defines it. The contrast of two colors is
        // (the brighter + 0.05) / (the darker + 0.05).
        public static double Luminance(Color color)
        {
            static double Linear(byte channel)
            {
                var value = channel / 255.0;
                return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }

        public static double Contrast(Color first, Color second)
        {
            var a = Luminance(first);
            var b = Luminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static byte Channel(byte from, byte to, double by) =>
            (byte)Math.Round(MathHelper.Lerp(from, to, by), MidpointRounding.AwayFromZero);
    }
}
