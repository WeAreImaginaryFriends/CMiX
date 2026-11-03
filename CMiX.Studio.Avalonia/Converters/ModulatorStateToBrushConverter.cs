// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using CMiX.Core.Modulation.Modulators;

namespace CMiX.Studio.Avalonia.Converters
{
    // Gives the brush of the assign button. The brush is found by name in the theme tokens:
    // Modulator + category + (Highlight when hovered) + Brush. No modulator gives ModulatorUnassignedBrush.
    // The binding values are the bound modulator and its IsHovered, so the brush follows both.
    public sealed class ModulatorStateToBrushConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 1)
                return AvaloniaProperty.UnsetValue;

            var key = values[0] is IModulator modulator
                ? $"Modulator{modulator.Category}{(modulator.IsHovered ? "Highlight" : "")}Brush"
                : "ModulatorUnassignedBrush";

            var application = Application.Current;
            if (application != null && application.TryGetResource(key, application.ActualThemeVariant, out var brush))
                return brush;

            return AvaloniaProperty.UnsetValue;
        }
    }
}
