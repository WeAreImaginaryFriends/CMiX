using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CMiX.UI.Tests
{
    // The row of the arrow keys in a context menu has the hover fill.
    public class ContextMenuKeyboardRowTests
    {
        private static ContextMenu Open(params MenuItem[] rows)
        {
            var menu = new ContextMenu
            {
                Theme = (ControlTheme)Application.Current!.FindResource("ContextMenuDefault")!,
                ItemsSource = rows,
            };
            var owner = new Border { Width = 200, Height = 30, ContextMenu = menu };
            new Window { Content = owner, Width = 500, Height = 400 }.Show();
            menu.Open(owner);
            Dispatcher.UIThread.RunJobs();
            return menu;
        }

        private static void Press(Control target, Key key, int times = 1)
        {
            for (var i = 0; i < times; i++)
            {
                var focused = TopLevel.GetTopLevel(target)?.FocusManager?.GetFocusedElement() as Interactive;
                (focused ?? target).RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
                Dispatcher.UIThread.RunJobs();
            }
        }

        private static IBrush? FillOf(MenuItem row) =>
            row.GetVisualDescendants().OfType<Border>().First(border => border.Name == "root").Background;

        private static Color HoverColor =>
            ((ISolidColorBrush)Application.Current!.FindResource("RowHoverBrush")!).Color;

        private static bool HasHoverFill(MenuItem row) =>
            FillOf(row) is ISolidColorBrush brush && brush.Color == HoverColor;

        [AvaloniaFact]
        public void Down_InAnOpenMenu_GivesTheRowTheHoverFill()
        {
            var first = new MenuItem { Header = "One" };
            var second = new MenuItem { Header = "Two" };
            var menu = Open(first, second);

            Press(menu, Key.Down);

            Assert.True(first.IsSelected);
            Assert.True(HasHoverFill(first));
            Assert.False(HasHoverFill(second));
        }

        [AvaloniaFact]
        public void Down_PastADisabledRow_SkipsItAndKeepsItsDimLook()
        {
            var first = new MenuItem { Header = "One" };
            var off = new MenuItem { Header = "Off", IsEnabled = false };
            var last = new MenuItem { Header = "Two" };
            var menu = Open(first, off, last);

            Press(menu, Key.Down, 2);

            Assert.True(last.IsSelected);
            Assert.False(off.IsSelected);
            Assert.False(HasHoverFill(off));
            Assert.Equal(0.5, off.GetVisualDescendants().OfType<Border>().First(border => border.Name == "root").Opacity);
        }

        [AvaloniaFact]
        public void Down_InAnOpenSubmenu_GivesTheRowTheHoverFill()
        {
            var inner = new MenuItem { Header = "Inner" };
            var more = new MenuItem { Header = "More", Items = { inner, new MenuItem { Header = "Other" } } };
            var menu = Open(more);

            Press(menu, Key.Down);
            Press(menu, Key.Right);

            Assert.True(more.IsSubMenuOpen);
            Assert.True(inner.IsSelected);
            Assert.True(HasHoverFill(inner));
        }
    }
}
