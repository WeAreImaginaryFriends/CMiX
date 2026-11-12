using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CMiX.UI.Tests
{
    // A clock that moves only when the test calls Advance. A timer runs its callback on the thread of the test when its time comes.
    // Only one shot timers are supported.
    public sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> timers = new();
        private DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state, now + dueTime);
            timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan time)
        {
            now += time;
            foreach (var timer in timers.Where(timer => timer.Due <= now).ToList())
            {
                timers.Remove(timer);
                timer.Fire();
            }
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider clock;
            private readonly TimerCallback callback;
            private readonly object? state;

            public ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state, DateTimeOffset due)
            {
                this.clock = clock;
                this.callback = callback;
                this.state = state;
                Due = due;
            }

            public DateTimeOffset Due { get; private set; }

            public void Fire() => callback(state);

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                Due = clock.now + dueTime;
                if (!clock.timers.Contains(this))
                    clock.timers.Add(this);
                return true;
            }

            public void Dispose() => clock.timers.Remove(this);

            public System.Threading.Tasks.ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }
}
