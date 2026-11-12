// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace CMiX.UI.AttachedProperties
{
    // Gives a control the pseudo class :popup-open while a popup of the control is open. Set it on the control:
    //     <UserControl ui:PopupOwner.IsEnabled="True">
    // A popup is of the control when the control is a logical ancestor of the popup or of its placement target: a context menu or
    // a flyout of a child, a submenu, or a panel that a menu opens. A popup lives in the overlay layer or in its own window,
    // out of the visual tree of the control. Thus :focus-within goes off while the popup has the focus, and :popup-open stays on.
    public static class PopupOwner
    {
        public const string PopupOpenPseudoClass = ":popup-open";

        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(PopupOwner));

        public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

        // The owners that each open popup marks. A popup can leave the logical tree before it closes, so the close clears these owners.
        private static readonly ConditionalWeakTable<Popup, List<Control>> owners = new();

        // The open popups of each owner.
        private static readonly ConditionalWeakTable<Control, HashSet<Popup>> openPopups = new();

        static PopupOwner()
        {
            Popup.IsOpenProperty.Changed.AddClassHandler<Popup>(OnIsOpenChanged);
        }

        private static void OnIsOpenChanged(Popup popup, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.GetNewValue<bool>())
                Mark(popup);
            else
                Unmark(popup);
        }

        private static void Mark(Popup popup)
        {
            Unmark(popup);

            var found = OwnersOf(popup).ToList();
            if (found.Count == 0)
                return;

            owners.AddOrUpdate(popup, found);
            foreach (var owner in found)
            {
                var set = openPopups.GetOrCreateValue(owner);
                set.Add(popup);
                Update(owner, set);
            }
        }

        private static void Unmark(Popup popup)
        {
            if (!owners.TryGetValue(popup, out var found))
                return;

            owners.Remove(popup);
            foreach (var owner in found)
            {
                var set = openPopups.GetOrCreateValue(owner);
                set.Remove(popup);
                Update(owner, set);
            }
        }

        private static IEnumerable<Control> OwnersOf(Popup popup)
        {
            var ancestors = popup.GetLogicalAncestors();
            if (popup.PlacementTarget is { } target)
                ancestors = ancestors.Concat(target.GetSelfAndLogicalAncestors());

            return ancestors.OfType<Control>().Where(GetIsEnabled).Distinct();
        }

        private static void Update(Control owner, HashSet<Popup> set) =>
            ((IPseudoClasses)owner.Classes).Set(PopupOpenPseudoClass, set.Count > 0);
    }
}
