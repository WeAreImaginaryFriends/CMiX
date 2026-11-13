using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CMiX.UI.Tests
{
    // A text box that has the keyboard focus has an accent border, the color of the focus line of a panel.
    public class TextBoxFocusLookTests
    {
        private static Color Token(string key) => ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

        private static (TextBox Box, Border Border) Show(string? themeKey)
        {
            var box = new TextBox { Text = "Text", Width = 120, Height = 24 };
            if (themeKey != null)
                box.Theme = (ControlTheme)Application.Current!.FindResource(themeKey)!;

            // The button takes the focus first, so the text box starts without it.
            var other = new Button { Content = "Other" };
            new Window { Content = new StackPanel { Children = { other, box } }, Width = 300, Height = 200 }.Show();
            other.Focus();
            Dispatcher.UIThread.RunJobs();
            return (box, box.GetVisualDescendants().OfType<Border>().First(part => part.Name == "border"));
        }

        [AvaloniaTheory]
        [InlineData(null)]
        [InlineData("TextBoxNumbersOnly")]
        public void Focus_ATextBox_GivesItTheAccentBorder(string? themeKey)
        {
            var (box, border) = Show(themeKey);
            Assert.NotEqual(Token("AccentBrush"), ((ISolidColorBrush)border.BorderBrush!).Color);

            box.Focus();
            Dispatcher.UIThread.RunJobs();

            Assert.True(box.IsFocused);
            Assert.Equal(Token("AccentBrush"), ((ISolidColorBrush)border.BorderBrush!).Color);
        }
    }
}
