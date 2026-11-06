// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Converters
{
    public class TypeNameToDisplayLabelConverter : IValueConverter
    {
        public static readonly TypeNameToDisplayLabelConverter Instance = new();

        // AddableTypes.LabelOf gives the label, so the panels and the menus of the tree agree.
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is Type type ? AddableTypes.LabelOf(type) : string.Empty;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
