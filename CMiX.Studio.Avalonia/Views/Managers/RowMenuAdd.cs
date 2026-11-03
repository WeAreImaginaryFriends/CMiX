// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;

namespace CMiX.Studio.Avalonia.Views.Managers
{
    // A row that adds something, at the top of the menu of a row (see PrefabRowMenu).
    // The id has the prefix of the menu, such as "row.add-layer".
    public sealed record RowMenuAdd(string Id, string Header, Action Execute);
}
