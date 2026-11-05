// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Mathematics;

namespace CMiX.Studio.Avalonia.Themes
{
    // The colors of a kind of row, for each state of the row.
    // The fills are the color of the kind, faint, over the background of the tree: they go behind the name.
    // The kinds are the color of the kind, stronger: they go in the count squares of a collapsed row.
    public sealed record CategoryTone(
        Color Fill,
        Color FillHover,
        Color FillSelected,
        Color Kind,
        Color KindHover,
        Color KindSelected)
    {
        // A color of the tone by its role. The XAML of the tree asks for a role by name (see TreeToneConverter).
        public Color Of(ToneRole role) => role switch
        {
            ToneRole.Fill => Fill,
            ToneRole.FillHover => FillHover,
            ToneRole.FillSelected => FillSelected,
            ToneRole.Kind => Kind,
            ToneRole.KindHover => KindHover,
            _ => KindSelected
        };
    }

    public enum ToneRole
    {
        Fill,
        FillHover,
        FillSelected,
        Kind,
        KindHover,
        KindSelected
    }

    // Gives the colors of a kind of row from the colors of the theme (Colors.axaml, the names Category + kind).
    // It takes a function for the colors, so a test can give its own colors without an application.
    public sealed class CategoryPalette
    {
        // The fill behind a row has the hue of its kind and the same brightness for every kind, so every kind is as strong as the others and
        // a text reads the same on all of them. The brightness is a relative luminance (see ColorMix.Luminance). A row deeper in the tree
        // by a change of kind has the brightness of the level above times LevelFactor, down to FloorLuminance. A hovered row and a
        // selected row have the brightness of the row at rest times a factor. The selected factor keeps a white text above a contrast of 4.5.
        public const double FillLuminance = 0.075;
        public const double LevelFactor = 0.8;
        public const double FloorLuminance = 0.02;
        public const double HoverFactor = 1.5;
        public const double SelectedFactor = 2.3;
        public const double MinSaturation = 0.55;
        public const double MaxSaturation = 0.85;

        // The strength of the color of a count square over the background, from 0 (the background) to 1 (the color of the kind).
        public const double KindStrength = 0.60;
        public const double KindHoverStrength = 0.85;
        public const double KindSelectedStrength = 1.0;

        // A composable inside a composable has one more color for each level, up to this level.
        public const int LastComposableLevel = 3;

        private readonly Func<string, Color> colorOf;
        private readonly Color background;

        public CategoryPalette(Func<string, Color> colorOf, Color background)
        {
            this.colorOf = colorOf;
            this.background = background;
        }

        // The palette of the running application: the colors of the theme over the background of the panels (Grey200).
        public static CategoryPalette FromApplication()
        {
            var resources = Application.Current!;
            Color Find(string key) => resources.TryFindResource(key, out var value) && value is Color color
                ? color
                : throw new InvalidOperationException($"The theme has no color named {key}.");

            return new CategoryPalette(Find, Find("Grey200"));
        }

        // The name of the color of a kind in the theme. The depth is the number of composables around a composable.
        public static string ColorKey(TreeCategory category, int depth = 0)
        {
            if (category != TreeCategory.Composable || depth <= 0)
                return "Category" + category;

            return "CategoryComposableLevel" + Math.Min(depth, LastComposableLevel);
        }

        // The tone of a row. The composable depth is the number of composables around it (the hue of a composable). The kind depth is the
        // number of changes of kind above it (the brightness): a group and its items have the same kind depth.
        public CategoryTone For(TreeCategory category, int composableDepth = 0, int kindDepth = 0)
        {
            var color = colorOf(ColorKey(category, composableDepth));
            var rest = Math.Max(FloorLuminance, FillLuminance * Math.Pow(LevelFactor, Math.Max(0, kindDepth)));
            Color Fill(double luminance) => ColorMix.WithLuminance(color, luminance, MinSaturation, MaxSaturation);

            return new CategoryTone(
                Fill(rest),
                Fill(rest * HoverFactor),
                Fill(rest * SelectedFactor),
                ColorMix.Over(color, background, KindStrength),
                ColorMix.Over(color, background, KindHoverStrength),
                ColorMix.Over(color, background, KindSelectedStrength));
        }
    }
}
