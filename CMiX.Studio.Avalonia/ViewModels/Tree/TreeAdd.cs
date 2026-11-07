// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // Something that can be added under a node of the tree. The id has the prefix of the menu, such as "row.add-layer".
    // A row with children is a submenu: it adds nothing itself, and its children add.
    // The category is the kind of what the row adds. The Add panel shows its color. It is null for a row that does not say it.
    // IconKey is the icon of the type that the row adds. It is null for a type that has no icon of its own.
    // Enabled is false for an entry that the node cannot take. The Add panel shows it greyed, and it has no action.
    public sealed record TreeAdd(string Id, string Header, Action? Execute = null, IReadOnlyList<TreeAdd>? Children = null, bool Secondary = false,
                                 TreeCategory? Category = null, string? IconKey = null, bool Enabled = true);
}
