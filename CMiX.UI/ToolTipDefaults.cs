// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CMiX.UI.Controls.Parts;

namespace CMiX.UI
{
    // Where every tooltip of the app opens: below the control that has it, 8 pixels away from it.
    // The popup of a tooltip has the room of the shadow around the box (see the ToolTip theme), so the offset is the
    // gap minus that room. A view does not set ToolTip.Placement or the offsets. It sets ToolTip.Tip only.
    // Avalonia moves the tooltip above the control when there is no room below.
    // The default value of the two properties cannot be changed for all controls, so a control gets them when it gets a tip.
    // A view can still set its own value for one control, and that value stays.
    public static class ToolTipDefaults
    {
        public const double Gap = 8;

        private static bool applied;

        // The theme calls this when it is made. A second call does nothing.
        public static void Apply()
        {
            if (applied)
                return;

            applied = true;
            ToolTip.TipProperty.Changed.AddClassHandler<Control>((control, e) =>
            {
                if (e.NewValue != null)
                    PlaceBelow(control);
            });
        }

        public static void PlaceBelow(Control control)
        {
            if (!control.IsSet(ToolTip.PlacementProperty))
                control.SetCurrentValue(ToolTip.PlacementProperty, PlacementMode.Bottom);

            if (!control.IsSet(ToolTip.VerticalOffsetProperty))
                control.SetCurrentValue(ToolTip.VerticalOffsetProperty, Gap - PopupFrame.ShadowMargin.Top);
        }
    }
}
