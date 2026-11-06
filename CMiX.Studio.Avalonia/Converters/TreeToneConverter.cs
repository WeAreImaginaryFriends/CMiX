// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Converters
{
    // The brush of a row of the tree for a role (a ToneRole name as the parameter): the fill behind a row, or the color of a count
    // square, for the state of the row. The value is a tree node or a count of a kind. A row of an unknown kind gets a clear brush,
    // so the row still takes the pointer.
    public sealed class TreeToneConverter : IValueConverter
    {
        private CategoryPalette? palette;

        // One brush for each kind, depth and role. The palette of an instance does not change, so the brushes stay valid.
        private readonly Dictionary<(TreeCategory Category, int ComposableDepth, int KindDepth, ToneRole Role), IBrush> brushes = new();

        // The number of brushes made. A test reads it.
        internal int BrushesMade => brushes.Count;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var (category, composableDepth, kindDepth) = value switch
            {
                TreeNode node => (node.Category, node.ComposableDepth, node.KindDepth),
                KindCount count => (count.Category, 0, 0),
                _ => ((TreeCategory?)null, 0, 0)
            };

            if (category == null || parameter is not string name || !Enum.TryParse<ToneRole>(name, out var role))
                return Brushes.Transparent;

            var key = (category.Value, composableDepth, kindDepth, role);
            if (brushes.TryGetValue(key, out var brush))
                return brush;

            palette ??= CategoryPalette.FromApplication();
            brush = new ImmutableSolidColorBrush(palette.For(category.Value, composableDepth, kindDepth).Of(role));
            brushes[key] = brush;
            return brush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
