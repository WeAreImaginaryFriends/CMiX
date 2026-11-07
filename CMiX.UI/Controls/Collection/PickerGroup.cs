// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace CMiX.UI.Controls.Collection
{
    // One row that can be picked in a GroupedSearchList. Value is what the owner gets back. Kind is any object that the list
    // gives to its KindBrush function, to color the small square before the text. It is null for a row with no color.
    public sealed record PickerItem(string Text, object? Value = null, object? Kind = null);

    // A group of rows under one header. Kind is the same as the kind of a row.
    public sealed record PickerGroup(string Header, IReadOnlyList<PickerItem> Items, object? Kind = null);

    // The row that the user picked, and its group.
    public sealed class PickerPickedEventArgs : EventArgs
    {
        public PickerPickedEventArgs(PickerGroup group, PickerItem item)
        {
            Group = group;
            Item = item;
        }

        public PickerGroup Group { get; }
        public PickerItem Item { get; }
    }
}
