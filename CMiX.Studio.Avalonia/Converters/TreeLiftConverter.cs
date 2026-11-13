// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CMiX.Studio.Avalonia.Converters
{
    // The ZIndex of a tree item: 1 for the cursor row and for each row above it, 0 for the others. The values are TreeNode.IsCursor
    // and TreeNode.IsOnCursorBranch. The cursor row is drawn after the rows around it then, so its shadow falls on them.
    public sealed class TreeLiftConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
                if (value is true)
                    return 1;

            return 0;
        }
    }
}
