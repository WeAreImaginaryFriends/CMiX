// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;

namespace CMiX.Studio.Avalonia.Views
{
    // The rows of a menu for the things that can be added under a node of the tree.
    internal static class TreeAddRows
    {
        public static IReadOnlyList<RowMenuAdd> ToRows(this IReadOnlyList<TreeAdd> adds) =>
            adds.Select(add => new RowMenuAdd(add.Id, add.Header, add.Execute, add.Children?.ToList().ToRows(), add.Secondary)).ToList();
    }
}
