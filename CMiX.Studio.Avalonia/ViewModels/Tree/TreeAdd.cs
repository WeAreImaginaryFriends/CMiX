// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // Something that can be added under a node of the tree. The id has the prefix of the menu, such as "row.add-layer".
    // A row with children is a submenu: it adds nothing itself, and its children add.
    public sealed record TreeAdd(string Id, string Header, Action? Execute = null, IReadOnlyList<TreeAdd>? Children = null, bool Secondary = false);
}
