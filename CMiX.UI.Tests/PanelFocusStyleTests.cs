using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace CMiX.UI.Tests
{
    // CMiXTheme has the panel focus line: a Border.focusline in a .focuspanel shows only while the panel has the keyboard focus.
    public class PanelFocusStyleTests
    {
        private static (Border Line, TextBox Inside, Button Outside) Show()
        {
            var line = new Border { Classes = { "focusline" } };
            var inside = new TextBox();
            var panel = new StackPanel { Classes = { "focuspanel" } };
            panel.Children.Add(line);
            panel.Children.Add(inside);

            var outside = new Button { Content = "Other" };
            var root = new StackPanel();
            root.Children.Add(panel);
            root.Children.Add(outside);
            new Window { Content = root, Width = 300, Height = 200 }.Show();
            Dispatcher.UIThread.RunJobs();
            return (line, inside, outside);
        }

        [AvaloniaFact]
        public void FocusLine_FocusInAndOut_ShowsTheAccentLineOnlyWithTheFocus()
        {
            var (line, inside, outside) = Show();

            outside.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.False(line.IsEffectivelyVisible);

            inside.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.True(line.IsEffectivelyVisible);
            Assert.Equal((double)Application.Current!.FindResource("SplitterIndicatorSize")!, line.Bounds.Height);
            Assert.Equal(
                ((ISolidColorBrush)Application.Current!.FindResource("AccentBrush")!).Color,
                ((ISolidColorBrush)line.Background!).Color);
        }
    }
}
