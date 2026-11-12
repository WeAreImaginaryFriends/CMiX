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
    // A key press in the window closes the tooltip. No tooltip opens again until the pointer moves more than WakeDistance.
    internal sealed class TreeTooltips
    {
        // The distance in pixels that the pointer must move after a key press before a tooltip can open again.
        internal const double WakeDistance = 4;

        private readonly DispatcherTimer timer = new();
        private readonly Control rows;
        private Control? hovered;
        private Control? open;
        private TopLevel? keySource;

        // The last place of the pointer over the rows.
        private Point? lastPoint;

        // The place of the pointer at the last key press. Null when no key press holds the tooltips back.
        private Point? keyPoint;

        // True from a key press until the pointer moves more than WakeDistance.
        private bool isMuted;

        public TreeTooltips(Control rows)
        {
            this.rows = rows;
            ToolTip.SetServiceEnabled(rows, false);
            rows.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            rows.AddHandler(InputElement.PointerPressedEvent, (_, _) => Hover(null), RoutingStrategies.Tunnel, handledEventsToo: true);
            rows.PointerExited += (_, _) => Hover(null);
            rows.AttachedToVisualTree += (_, _) => ListenToKeys(TopLevel.GetTopLevel(rows));
            rows.DetachedFromVisualTree += (_, _) => ListenToKeys(null);
            ListenToKeys(TopLevel.GetTopLevel(rows));
            timer.Tick += (_, _) => Show();
        }

        // The control whose tooltip waits to open or is open. A test reads it.
        internal Control? Hovered => hovered;

        // The control of the open tooltip.
        internal Control? Open => open;

        // True while a key press holds the tooltips back. A test reads it.
        internal bool IsMuted => isMuted;

        // The keys of the whole window close the tooltip, also the keys that a key binding or the tree handles.
        private void ListenToKeys(TopLevel? topLevel)
        {
            if (ReferenceEquals(topLevel, keySource))
                return;

            keySource?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            keySource = topLevel;
            keySource?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e) => Mute();

        // A key press: the tooltip closes, and a tooltip that waits does not open.
        internal void Mute()
        {
            Hover(null);
            isMuted = true;
            keyPoint = lastPoint;
        }

        private void OnMoved(object? sender, PointerEventArgs e)
        {
            var point = e.GetPosition(rows);
            lastPoint = point;
            if (isMuted)
            {
                if (keyPoint is not { } from)
                {
                    keyPoint = point;
                    return;
                }

                var delta = point - from;
                if (delta.X * delta.X + delta.Y * delta.Y <= WakeDistance * WakeDistance)
                    return;

                isMuted = false;
                keyPoint = null;
            }

            Hover(HostOf(e.Source as Visual));
        }

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
