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
        Color KindSelected);

    // Gives the colors of a kind of row from the colors of the theme (Colors.axaml, the names Category + kind).
    // It takes a function for the colors, so a test can give its own colors without an application.
    public sealed class CategoryPalette
    {
        // The strength of each color over the background, from 0 (the background) to 1 (the color of the kind).
        public const double FillStrength = 0.20;
        public const double FillHoverStrength = 0.27;
        public const double FillSelectedStrength = 0.36;
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

        public CategoryTone For(TreeCategory category, int depth = 0)
        {
            var color = colorOf(ColorKey(category, depth));
            return new CategoryTone(
                ColorMix.Over(color, background, FillStrength),
                ColorMix.Over(color, background, FillHoverStrength),
                ColorMix.Over(color, background, FillSelectedStrength),
                ColorMix.Over(color, background, KindStrength),
                ColorMix.Over(color, background, KindHoverStrength),
                ColorMix.Over(color, background, KindSelectedStrength));
        }
    }
}
