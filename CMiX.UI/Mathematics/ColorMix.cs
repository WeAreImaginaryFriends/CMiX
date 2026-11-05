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

        // The color with the hue of the given color, a saturation kept between two limits, and the given luminance.
        // Two colors of different hues get the same brightness, so they look as strong as each other, and a text on them reads the same.
        // The result is the closest the lightness can come to the luminance: a luminance that no lightness reaches gives the nearest end.
        public static Color WithLuminance(Color color, double luminance, double minSaturation, double maxSaturation)
        {
            var (hue, saturation, _) = ToHsl(color);
            saturation = Math.Clamp(saturation, minSaturation, maxSaturation);

            double low = 0, high = 1;
            for (var step = 0; step < 32; step++)
            {
                var middle = (low + high) / 2;
                if (Luminance(FromHsl(hue, saturation, middle)) < luminance)
                    low = middle;
                else
                    high = middle;
            }

            return FromHsl(hue, saturation, (low + high) / 2);
        }

        private static (double Hue, double Saturation, double Lightness) ToHsl(Color color)
        {
            double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var lightness = (max + min) / 2;
            if (max == min)
                return (0, 0, lightness);

            var delta = max - min;
            var saturation = lightness > 0.5 ? delta / (2 - max - min) : delta / (max + min);
            var hue = max == r ? (g - b) / delta + (g < b ? 6 : 0)
                : max == g ? (b - r) / delta + 2
                : (r - g) / delta + 4;

            return (hue * 60, saturation, lightness);
        }

        private static Color FromHsl(double hue, double saturation, double lightness)
        {
            var chroma = saturation * Math.Min(lightness, 1 - lightness);
            double Channel(int n)
            {
                var k = (n + hue / 30) % 12;
                return lightness - chroma * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1));
            }

            static byte ToByte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255, MidpointRounding.AwayFromZero);
            return Color.FromRgb(ToByte(Channel(0)), ToByte(Channel(8)), ToByte(Channel(4)));
        }

        private static byte Channel(byte from, byte to, double by) =>
            (byte)Math.Round(MathHelper.Lerp(from, to, by), MidpointRounding.AwayFromZero);
    }
}
