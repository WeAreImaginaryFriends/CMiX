// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CMiX.Studio.Avalonia.Converters
{
    // True when the cover of a tree row is on. The values are TreeNode.IsDimmed, TreeNode.IsSearchDimmed and TreeNode.IsDragActive,
    // in this order. A drag dims the rows that can not take the node. While no drag runs, the search dims the rows that do not match.
    // During a drag, only the drag dims, and the search look comes back when the drag ends.
    public sealed class TreeCoverConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var dragDimmed = values.Count > 0 && values[0] is true;
            var searchDimmed = values.Count > 1 && values[1] is true;
            var dragActive = values.Count > 2 && values[2] is true;
            return dragDimmed || (searchDimmed && !dragActive);
        }
    }
}
