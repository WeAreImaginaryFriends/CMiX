// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using CMiX.UI.Themes;

namespace CMiX.UI.Popups
{
    // The place of the one toast of a window: at the bottom center of its area, over the other controls.
    // Show puts a new toast in place of the old one. The toast hides itself after Duration.
    // The host and its toast take no focus and no pointer input, so a click goes to the control under them.
    public class ToastHost : Decorator
    {
        public static readonly StyledProperty<TimeSpan> DurationProperty =
            AvaloniaProperty.Register<ToastHost, TimeSpan>(nameof(Duration), Motion.ToastDuration);

        // The time that a toast shows.
        public TimeSpan Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        private readonly Toast toast = new();
        private ITimer? timer;

        // The number of the toast that shows. A timer of an older toast does not hide a newer one.
        private int shown;

        public ToastHost()
        {
            Focusable = false;
            IsHitTestVisible = false;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Bottom;
            this.Bind(MarginProperty, this.GetResourceObservable("ToastMargin"));
            toast.IsVisible = false;
            Child = toast;
        }

        // The clock of the hide timer. A test sets a clock that it moves by hand.
        public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

        public Toast Toast => toast;

        public bool IsShowing => toast.IsVisible;

        // Shows the text, and the undo gesture as a key cap when it is not null. The toast replaces the toast that shows.
        public void Show(string text, string? undoGesture = null)
        {
            Dispatcher.UIThread.VerifyAccess();
            toast.Text = text;
            toast.UndoGesture = undoGesture;
            toast.IsVisible = true;

            // The timer posts the hide to the dispatcher of the host. It never posts to a dispatcher that comes later (a new test).
            var number = ++shown;
            var dispatcher = Dispatcher.UIThread;
            timer?.Dispose();
            timer = TimeProvider.CreateTimer(
                _ => dispatcher.Post(() => { if (number == shown) Hide(); }),
                null, Duration, Timeout.InfiniteTimeSpan);
        }

        // Stops the hide timer when the host leaves the window. A hide that the timer posted before then does nothing.
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            shown++;
            timer?.Dispose();
            timer = null;
            base.OnDetachedFromVisualTree(e);
        }

        public void Hide()
        {
            timer?.Dispose();
            timer = null;
            toast.IsVisible = false;
        }
    }
}
