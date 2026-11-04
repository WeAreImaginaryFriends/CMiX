// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using CMiX.Core.Modulation.Modulators;
using CMiX.Studio.Avalonia.Views.Modulation;

namespace CMiX.Studio.Avalonia.Converters
{
    // Gives the tooltip of the assign button: the modulator and the output that are assigned, as the menu names them
    // ("Random Value"). With nothing assigned it gives the title of the menu.
    // The binding values are the bound modulator, the name of the bound output, and the name of the modulator.
    // The last one is only there to refresh the tooltip when the modulator is renamed.
    public sealed class ModulatorToToolTipConverter : IMultiValueConverter
    {
        public const string Unassigned = "Assign Modulator";

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 || values[0] is not IModulator modulator || values[1] is not string outputName || outputName.Length == 0)
                return Unassigned;

            var output = modulator.Outputs.FirstOrDefault(candidate => candidate.Name == outputName);
            return ModulatorOutputChoice.LabelOf(modulator, output);
        }
    }
}
