using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Styling;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls;
using CMiX.UI.Controls.Parts;
using Xunit;

namespace CMiX.UI.Tests
{
    // The look of a menu: the card around it, and the rows with a shortcut, an arrow, a title or a warning.
    public class ContextMenuRowLookTests
    {
        private static (ContextMenu Menu, Dictionary<string, MenuItem> Rows) Open()
        {
            var add = new MenuItem { Header = "Add", InputGesture = new KeyGesture(Key.A, KeyModifiers.Shift) };
            add.Classes.Add("opens");
            var title = new MenuItem { Header = "Layer 02", IsEnabled = false };
            title.Classes.Add("title");
            var delete = new MenuItem { Header = "Delete", InputGesture = new KeyGesture(Key.Delete) };
            delete.Classes.Add("danger");
            var submenu = new MenuItem { Header = "More", Items = { new MenuItem { Header = "One" } } };
            var menu = new ContextMenu
            {
                Theme = (ControlTheme)Application.Current!.FindResource("ContextMenuDefault")!,
                ItemsSource = new object[]
                {
                    title,
                    new MenuItem { Header = "-" },
                    add,
                    new MenuItem { Header = "Duplicate", InputGesture = new KeyGesture(Key.D, KeyModifiers.Control) },
                    new MenuItem { Header = "Rename" },
                    submenu,
                    delete,
                },
            };
            var owner = new Border { Width = 200, Height = 30, ContextMenu = menu };
            new Window { Content = owner, Width = 500, Height = 400 }.Show();
            menu.Open(owner);
            Dispatcher.UIThread.RunJobs();

            var rows = menu.Items.OfType<MenuItem>().Where(item => item.Header is string header && header != "-")
                .ToDictionary(item => (string)item.Header!);
            return (menu, rows);
        }

        private static T Part<T>(MenuItem row, string name) where T : Control =>
            row.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

        [AvaloniaFact]
        public void Open_AMenu_ShowsItsRowsOnTheCard()
        {
            var (menu, _) = Open();

            var frame = menu.GetVisualDescendants().OfType<PopupFrame>().First();
            var card = frame.GetVisualDescendants().OfType<Border>().First(border => border.BorderThickness == new Thickness(1) && border.Background != null);

            var fill = ((ISolidColorBrush)Application.Current!.FindResource("PopupCardBackgroundBrush")!).Color;
            var line = ((ISolidColorBrush)Application.Current!.FindResource("PopupCardBorderBrush")!).Color;
            Assert.Equal(fill, ((ISolidColorBrush)card.Background!).Color);
            Assert.Equal(line, ((ISolidColorBrush)card.BorderBrush!).Color);
            Assert.Equal(1, card.Opacity);
        }

        [AvaloniaFact]
        public void Open_ARowWithAShortcut_ShowsItInTheMonoFont()
        {
            var (_, rows) = Open();

            var gesture = Part<TextBlock>(rows["Duplicate"], "PART_InputGestureText");

            Assert.True(gesture.IsEffectivelyVisible);
            Assert.Equal("Ctrl+D", gesture.Text);
            Assert.Equal((FontFamily)Application.Current!.FindResource("FontFamilyMono")!, gesture.FontFamily);
        }

        [AvaloniaFact]
        public void Open_ARowWithoutAShortcut_ShowsNone()
        {
            var (_, rows) = Open();

            Assert.False(Part<TextBlock>(rows["Rename"], "PART_InputGestureText").IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Open_TheShortcuts_LineUpAcrossTheRows()
        {
            var (menu, rows) = Open();

            double LeftOf(string header) => Part<TextBlock>(rows[header], "PART_InputGestureText").TranslatePoint(default, menu)!.Value.X;

            Assert.Equal(LeftOf("Add"), LeftOf("Duplicate"), 0.51);
            Assert.Equal(LeftOf("Add"), LeftOf("Delete"), 0.51);
        }

        [AvaloniaFact]
        public void Open_ARowWithASubmenu_ShowsTheArrow()
        {
            var (_, rows) = Open();

            Assert.True(Part<AppIcon>(rows["More"], "PART_Arrow").IsEffectivelyVisible);
            Assert.False(Part<AppIcon>(rows["Rename"], "PART_Arrow").IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Open_ARowOfTheClassOpens_ShowsTheArrowWithoutASubmenu()
        {
            var (_, rows) = Open();

            Assert.True(Part<AppIcon>(rows["Add"], "PART_Arrow").IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Open_TheTitleRow_IsQuietAndNotDimmed()
        {
            var (_, rows) = Open();

            var root = Part<Border>(rows["Layer 02"], "root");
            var header = Part<ContentPresenter>(rows["Layer 02"], "PART_HeaderPresenter");
            var quiet = ((ISolidColorBrush)Application.Current!.FindResource("Grey800Brush")!).Color;
            Assert.Equal(1, root.Opacity);
            Assert.Equal(quiet, ((ISolidColorBrush)header.Foreground!).Color);
            Assert.Equal(FontWeight.Medium, header.FontWeight);
        }

        [AvaloniaFact]
        public void Open_ARowOfTheClassDanger_HasTheWarningColor()
        {
            var (_, rows) = Open();

            var header = Part<ContentPresenter>(rows["Delete"], "PART_HeaderPresenter");

            var danger = ((ISolidColorBrush)Application.Current!.FindResource("PopupCardDangerBrush")!).Color;
            Assert.Equal(danger, ((ISolidColorBrush)header.Foreground!).Color);
        }

        [AvaloniaFact]
        public void Open_ADisabledRowThatIsNotATitle_IsDimmed()
        {
            var menu = new ContextMenu
            {
                Theme = (ControlTheme)Application.Current!.FindResource("ContextMenuDefault")!,
                ItemsSource = new object[] { new MenuItem { Header = "Off", IsEnabled = false } },
            };
            var owner = new Border { Width = 200, Height = 30, ContextMenu = menu };
            new Window { Content = owner, Width = 500, Height = 400 }.Show();
            menu.Open(owner);
            Dispatcher.UIThread.RunJobs();

            var row = menu.Items.OfType<MenuItem>().Single();

            Assert.Equal(0.5, Part<Border>(row, "root").Opacity);
        }
    }
}
