// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace CMiX.Studio.Avalonia.Views
{
    // Shows the tooltips of the rows of the tree. The tooltip service of Avalonia does not open them in the running app, while it opens
    // the tooltips of the toolbar (the log of the pointer shows that the right control is found and that nothing then opens). So the tree
    // switches the service off for its rows and does the same work: the tooltip of the control under the pointer opens after its show delay,
    // and it closes when the pointer goes to another control, leaves the tree, or presses a button.
    internal sealed class TreeTooltips
    {
        private readonly DispatcherTimer timer = new();
        private Control? hovered;
        private Control? open;

        public TreeTooltips(Control rows)
        {
            ToolTip.SetServiceEnabled(rows, false);
            rows.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            rows.AddHandler(InputElement.PointerPressedEvent, (_, _) => Hover(null), RoutingStrategies.Tunnel, handledEventsToo: true);
            rows.PointerExited += (_, _) => Hover(null);
            timer.Tick += (_, _) => Show();
        }

        // The control whose tooltip waits to open or is open. A test reads it.
        internal Control? Hovered => hovered;

        // The control of the open tooltip.
        internal Control? Open => open;

        private void OnMoved(object? sender, PointerEventArgs e) => Hover(HostOf(e.Source as Visual));

        // The nearest control from the one under the pointer up that has a tooltip and can show it.
        internal static Control? HostOf(Visual? visual)
        {
            for (var current = visual; current != null; current = current.GetVisualParent())
                if (current is Control control && ToolTip.GetTip(control) != null && (control.IsEffectivelyEnabled || ToolTip.GetShowOnDisabled(control)))
                    return control;

            return null;
        }

        // The pointer is on a control, or on none. A new control closes the open tooltip and starts the wait of its own.
        internal void Hover(Control? host)
        {
            if (ReferenceEquals(host, hovered))
                return;

            timer.Stop();
            Close();
            hovered = host;
            if (host == null)
                return;

            var delay = ToolTip.GetShowDelay(host);
            if (delay <= 0)
            {
                Show();
                return;
            }

            timer.Interval = TimeSpan.FromMilliseconds(delay);
            timer.Start();
        }

        // The wait is over: the tooltip of the control under the pointer opens.
        internal void Show()
        {
            timer.Stop();
            if (hovered is not { } host || !host.IsAttachedToVisualTree())
                return;

            ToolTip.SetIsOpen(host, true);
            open = host;
        }

        private void Close()
        {
            if (open == null)
                return;

            ToolTip.SetIsOpen(open, false);
            open = null;
        }
    }
}
