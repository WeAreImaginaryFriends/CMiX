using CMiX.Studio.Avalonia.Platform;

namespace CMiX.Studio.Avalonia.Tests
{
    // Counts the calls of a drag wheel source. It sets no hook.
    public sealed class FakeDragWheelSource : IDragWheelSource
    {
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsActive { get; private set; }

        public void Start(Action<int> onWheel)
        {
            StartCount++;
            IsActive = true;
        }

        public void Stop()
        {
            StopCount++;
            IsActive = false;
        }

        public void Dispose()
        {
            Stop();
            IsDisposed = true;
        }
    }
}
