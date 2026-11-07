// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // How many items of one kind a row holds. A collapsed row shows one of these for each kind it holds.
    public sealed record KindCount(TreeCategory Category, int Count)
    {
        // The text of the tooltip of the square, the name of the kind such as "Entity". It does not tell how many.
        public string Description => TreeCategories.Label(Category, 1);
    }
}
