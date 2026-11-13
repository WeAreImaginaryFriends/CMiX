using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The colors of the sync dots in the main window and in the sync settings.
    public class StatusIndicatorTests
    {
        private static Ellipse Dot(string themeKey)
        {
            var dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Theme = (ControlTheme)Application.Current!.FindResource(themeKey)!
            };
            var window = new Window { Content = dot, Width = 50, Height = 50 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return dot;
        }

        // A color of the theme (Colors.axaml).
        private static Color Token(string key) => (Color)Application.Current!.FindResource(key)!;

        private static Color ColorOf(Ellipse dot)
        {
            Dispatcher.UIThread.RunJobs();
            return ((ISolidColorBrush)dot.Fill!).Color;
        }

        [AvaloniaFact]
        public void ConnectionDot_IsRedWithoutAConnection()
        {
            Assert.Equal(Token("PopupCardDanger"), ColorOf(Dot("EllipseSyncConnection")));
        }

        [AvaloniaFact]
        public void ConnectionDot_IsGreenWhenConnected()
        {
            var dot = Dot("EllipseSyncConnection");
            dot.Classes.Add("connected");

            Assert.Equal(Token("StatusSuccess"), ColorOf(dot));
        }

        [AvaloniaFact]
        public void ConnectionDot_IsGrayWhileConnecting_EvenWhenConnected()
        {
            var dot = Dot("EllipseSyncConnection");
            dot.Classes.Add("connected");
            dot.Classes.Add("connecting");

            Assert.Equal(Token("Grey800"), ColorOf(dot));
        }

        [AvaloniaFact]
        public void ConnectionDot_IsOrangeWhenConnectedButNotInSync()
        {
            var dot = Dot("EllipseSyncConnection");
            dot.Classes.Add("connected");
            dot.Classes.Add("unsynced");

            Assert.Equal(Token("PopupCardWarning"), ColorOf(dot));
        }

        [AvaloniaFact]
        public void ConnectionDot_IsRed_WhenNotInSyncAndNotConnected()
        {
            var dot = Dot("EllipseSyncConnection");
            dot.Classes.Add("unsynced");

            Assert.Equal(Token("PopupCardDanger"), ColorOf(dot));
        }

        [AvaloniaFact]
        public void ConnectionDot_IsGrayWhileConnecting_EvenWhenNotInSync()
        {
            var dot = Dot("EllipseSyncConnection");
            dot.Classes.Add("connected");
            dot.Classes.Add("unsynced");
            dot.Classes.Add("connecting");

            Assert.Equal(Token("Grey800"), ColorOf(dot));
        }

        // A dot is a small shape with no text: it needs a contrast of 3:1 or more on the window and on the panels.
        [AvaloniaTheory]
        [InlineData("StatusDisconnectedBrush")]
        [InlineData("StatusConnectedBrush")]
        [InlineData("StatusUnsyncedBrush")]
        [InlineData("StatusPendingBrush")]
        [InlineData("StatusSyncedBrush")]
        public void ADot_OnTheWindowAndOnAPanel_CanBeSeen(string brush)
        {
            var dot = ((ISolidColorBrush)Application.Current!.FindResource(brush)!).Color;

            foreach (var surface in new[] { "Grey100", "Grey200", "Grey300", "Grey400" })
                Assert.True(CMiX.UI.Mathematics.ColorMix.Contrast(dot, Token(surface)) >= 3, $"{brush} on {surface}: {CMiX.UI.Mathematics.ColorMix.Contrast(dot, Token(surface)):0.00}");
        }

        [AvaloniaFact]
        public void StateDot_IsGrayUntilInSync_ThenBlue()
        {
            var dot = Dot("EllipseSyncState");
            Assert.Equal(Token("Grey800"), ColorOf(dot));

            dot.Classes.Add("insync");

            Assert.Equal(Token("AccentPrimary"), ColorOf(dot));
        }
    }
}
