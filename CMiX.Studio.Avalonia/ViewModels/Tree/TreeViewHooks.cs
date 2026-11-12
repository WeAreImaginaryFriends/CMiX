// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // What one tree view tells the tree and does for it (see ProjectTree.AttachView). Each view has its own hooks, so two views can
    // show the same tree. The names are the names of the hooks of ProjectTree.
    public sealed class TreeViewHooks
    {
        public Func<bool> IsPointerOverTree { get; init; } = () => false;

        public Func<bool> IsTreeFocused { get; init; } = () => false;

        public Func<bool> IsTextBoxFocused { get; init; } = () => false;

        public Func<bool> IsFocusInOtherPanel { get; init; } = () => false;

        // True when the focused control is in the side panel of the view: the rows, the search box or the bar.
        public Func<bool> IsPanelFocused { get; init; } = () => false;

        public Action ToggleDragRow { get; init; } = () => { };

        public Action CancelDrag { get; init; } = () => { };

        public Action<TreeNode?, Func<IReadOnlyList<TreeNode>>> CloseRowsInView { get; init; } = (_, close) => close();
    }
}
