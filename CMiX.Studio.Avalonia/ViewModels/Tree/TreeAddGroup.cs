// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // A group of things that can be added under a node of the tree, such as Content or Filter. The Add panel shows one header for each
    // group. The id has the prefix "group.". The category is the kind of the items, or null when the items have different kinds.
    // The items add one thing each, and their headers have no "Add" in front.
    public sealed record TreeAddGroup(string Id, string Header, TreeCategory? Category, IReadOnlyList<TreeAdd> Items);
}
