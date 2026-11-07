// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using CMiX.Studio.Avalonia.ViewModels.Tree;

namespace CMiX.Studio.Avalonia.Views.Managers
{
    // A row that adds one thing, at the top of the menu of a row (see PrefabRowMenu).
    // The id has the prefix of the menu, such as "row.add-composition".
    public sealed record RowMenuAdd(string Id, string Header, Action? Execute = null);

    // The Add row of a menu. It opens the Add panel beside the menu, with the groups that Groups gives when the row opens it.
    // One panel can serve many menus, because only one menu is open at a time.
    // Primary is true when the Add row is the main use of the menu, so the menu opens on it (see MenuAnchor). It is false for a row that is
    // more often duplicated or renamed than filled. OpensAtOnce is true when the panel opens as soon as the menu opens (the key Tree.Add).
    public sealed record AddMenuRow(AddPanel Panel, Func<IReadOnlyList<TreeAddGroup>> Groups, bool Primary = true, bool OpensAtOnce = false);
}
