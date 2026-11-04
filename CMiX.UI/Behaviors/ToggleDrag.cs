// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls.Primitives;

namespace CMiX.UI.Behaviors
{
    // Marks a toggle button as part of a group that a drag can paint (see ToggleDragBehavior).
    // A drag that starts on a button paints only the buttons with the same group name.
    public static class ToggleDrag
    {
        public static readonly AttachedProperty<string?> GroupProperty =
            AvaloniaProperty.RegisterAttached<ToggleButton, string?>("Group", typeof(ToggleDrag));

        public static string? GetGroup(ToggleButton button) => button.GetValue(GroupProperty);

        public static void SetGroup(ToggleButton button, string? value) => button.SetValue(GroupProperty, value);
    }
}
