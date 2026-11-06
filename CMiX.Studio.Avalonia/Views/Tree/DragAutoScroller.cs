// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia.Controls;
using Avalonia.Threading;
using CMiX.Studio.Avalonia.Platform;

namespace CMiX.Studio.Avalonia.Views
{
    // Scrolls a view during a drag. A timer scrolls while the pointer is near the top or bottom edge.
    // The wheel source gives the mouse wheel, which the system does not send during a drag. A wheel step is three rows.
    // This class is the only owner of the timer and of the wheel source.
    public sealed class DragAutoScroller : IDisposable
    {
        private const double EdgeZone = 28.0;
        private const double MaxScrollStep = 18.0;

        private readonly IDragWheelSource wheelSource;
        private readonly Func<ScrollViewer?> scroller;
        private readonly Func<double> rowPitch;
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(30) };
        private double? dragY;
        private object? pendingStop;
        private bool disposed;

        // scroller gives the scroll viewer of the view. rowPitch gives the height of a row plus the space under it.
        public DragAutoScroller(IDragWheelSource wheelSource, Func<ScrollViewer?> scroller, Func<double> rowPitch)
        {
            this.wheelSource = wheelSource;
            this.scroller = scroller;
            this.rowPitch = rowPitch;
            timer.Tick += (_, _) => AutoScrollStep();
        }

        // True while the timer runs.
        public bool IsTimerRunning => timer.IsEnabled;

        // True while the timer runs or the wheel source is active.
        public bool IsRunning => timer.IsEnabled || wheelSource.IsActive;

        // The pointer of a file drag, in the coordinates of the scroll viewer. Null when the pointer is not known.
        // The system sends no wheel during a file drag, so the wheel source starts. A Track cancels a stop that StopDeferred posted.
        public void Track(double? y) => Track(y, true);

        // The pointer of a row drag. The wheel of a row drag comes as a pointer event (see TreeRowDrag), so the wheel source stays off.
        public void TrackRows(double? y) => Track(y, false);

        private void Track(double? y, bool withWheelSource)
        {
            if (disposed)
                return;

            pendingStop = null;
            dragY = y;
            if (withWheelSource)
                wheelSource.Start(steps => Dispatcher.UIThread.Post(() => ScrollByWheel(steps)));

            if (!timer.IsEnabled)
                timer.Start();
        }

        // Stops the timer at once and the wheel source one dispatcher pass later.
        // A leave of one row comes just before the enter of the next row, so the next Track cancels the stop.
        public void StopDeferred()
        {
            timer.Stop();
            var token = new object();
            pendingStop = token;
            Dispatcher.UIThread.Post(() =>
            {
                if (ReferenceEquals(pendingStop, token))
                    StopNow();
            });
        }

        // Stops the timer and the wheel source at once.
        public void StopNow()
        {
            pendingStop = null;
            timer.Stop();
            wheelSource.Stop();
            dragY = null;
        }

        public void ScrollByWheel(int steps)
        {
            if (dragY == null || scroller() is not { } view)
                return;

            var maxOffset = Math.Max(0, view.Extent.Height - view.Viewport.Height);
            var rows = steps * 3 * rowPitch();
            view.Offset = view.Offset.WithY(Math.Clamp(view.Offset.Y - rows, 0, maxOffset));
        }

        // One step of the scroll of a drag. The step is larger when the pointer is closer to the edge, or beyond it.
        // Returns the distance that the view moved.
        public double AutoScrollStep()
        {
            if (dragY is not { } y || scroller() is not { } view)
                return 0;

            var step = 0.0;
            if (y < EdgeZone)
                step = -MaxScrollStep * Math.Min(1, (EdgeZone - y) / EdgeZone);
            else if (y > view.Bounds.Height - EdgeZone)
                step = MaxScrollStep * Math.Min(1, (y - (view.Bounds.Height - EdgeZone)) / EdgeZone);

            if (step == 0)
                return 0;

            var before = view.Offset.Y;
            var maxOffset = Math.Max(0, view.Extent.Height - view.Viewport.Height);
            view.Offset = view.Offset.WithY(Math.Clamp(before + step, 0, maxOffset));
            return view.Offset.Y - before;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            StopNow();
            wheelSource.Dispose();
            disposed = true;
        }
    }
}
