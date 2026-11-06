using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // A fact that runs on Windows only. On another system it is skipped with the reason.
    public sealed class WindowsFactAttribute : FactAttribute
    {
        public WindowsFactAttribute(string reason)
        {
            if (!OperatingSystem.IsWindows())
                Skip = reason;
        }
    }
}
