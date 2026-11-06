using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CMiX.Studio.Avalonia.Views;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The scroll of a drag stops its wheel source one dispatcher pass after a leave, unless the drag comes back.
    public class DragAutoScrollerTests
    {
        private static (DragAutoScroller Scroller, FakeDragWheelSource Source) Create()
        {
            var source = new FakeDragWheelSource();
            return (new DragAutoScroller(source, () => null, () => 27), source);
        }

        [AvaloniaFact]
        public void StopDeferred_WithoutANewTrack_StopsTheWheelSourceAfterOneDispatcherPass()
        {
            var (scroller, source) = Create();
            scroller.Track(10);
            Assert.True(scroller.IsTimerRunning);

            scroller.StopDeferred();
            Assert.False(scroller.IsTimerRunning);
            Assert.True(source.IsActive);
            Assert.Equal(0, source.StopCount);

            Dispatcher.UIThread.RunJobs();
            Assert.Equal(1, source.StopCount);
            Assert.False(scroller.IsRunning);
        }

        [AvaloniaFact]
        public void StopDeferred_FollowedByTrack_KeepsTheWheelSourceRunning()
        {
            var (scroller, source) = Create();
            scroller.Track(10);
            scroller.StopDeferred();
            scroller.Track(12);

            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, source.StopCount);
            Assert.True(source.IsActive);
            Assert.True(scroller.IsTimerRunning);
            scroller.StopNow();
        }

        [AvaloniaFact]
        public void Dispose_WhileRunning_StopsAndDisposesTheWheelSource()
        {
            var (scroller, source) = Create();
            scroller.Track(10);

            scroller.Dispose();
            Assert.False(scroller.IsRunning);
            Assert.False(source.IsActive);
            Assert.True(source.IsDisposed);
        }
    }
}
