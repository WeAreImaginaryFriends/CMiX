// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
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

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var (category, depth) = value switch
            {
                TreeNode node => (node.Category, node.ComposableDepth),
                KindCount count => (count.Category, 0),
                _ => ((TreeCategory?)null, 0)
            };

            if (category == null || parameter is not string name || !Enum.TryParse<ToneRole>(name, out var role))
                return Brushes.Transparent;

            palette ??= CategoryPalette.FromApplication();
            return new ImmutableSolidColorBrush(palette.For(category.Value, depth).Of(role));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
