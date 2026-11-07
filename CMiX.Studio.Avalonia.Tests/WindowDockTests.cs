using Avalonia;
using CMiX.Studio.Avalonia.Views;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The main window opens on the right edge of the screen, as high as the working area.
    public class WindowDockTests
    {
        [Fact]
        public void Compute_AScreen_PutsTheWindowOnTheRightEdgeAtFullHeight()
        {
            var (position, width, height) = WindowDock.Compute(new PixelRect(0, 0, 1920, 1040), 1, 908);

            Assert.Equal(new PixelPoint(1920 - 908, 0), position);
            Assert.Equal(908, width);
            Assert.Equal(1040, height);
        }

        [Fact]
        public void Compute_AScaledScreen_UsesDeviceIndependentSizes()
        {
            var (position, width, height) = WindowDock.Compute(new PixelRect(1920, 40, 2880, 1560), 1.5, 908);

            Assert.Equal(new PixelPoint(1920 + 2880 - 1362, 40), position);
            Assert.Equal(908, width);
            Assert.Equal(1040, height);
        }

        [Fact]
        public void Compute_ANarrowScreen_KeepsTheWindowOnTheScreen()
        {
            var (position, width, _) = WindowDock.Compute(new PixelRect(0, 0, 800, 600), 1, 908);

            Assert.Equal(new PixelPoint(0, 0), position);
            Assert.Equal(800, width);
        }
    }
}
