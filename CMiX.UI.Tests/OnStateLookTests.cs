using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Mathematics;
using Xunit;

namespace CMiX.UI.Tests
{
    // A control that is on and a row that is selected lie on the accent: AccentInactiveBrush at rest, StateOnHoverBrush under the pointer.
    // Their text is ActiveForegroundBrush, so it can be read on the two fills.
    public class OnStateLookTests
    {
        private static Color Token(string key) => ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

        private static void Show(Control control)
        {
            new Window { Content = control, Width = 300, Height = 200 }.Show();
            Dispatcher.UIThread.RunJobs();
        }

        private static Color TextColorOf(Control control) =>
            ((ISolidColorBrush)control.GetVisualDescendants().OfType<TextBlock>().First().Foreground!).Color;

        private static Color FillOf(Control control, string part) =>
            ((ISolidColorBrush)control.GetVisualDescendants().OfType<Border>().First(border => border.Name == part).Background!).Color;

        private static void Hover(Control control)
        {
            foreach (var part in control.GetSelfAndVisualDescendants().OfType<Control>())
                ((IPseudoClasses)part.Classes).Set(":pointerover", true);
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaTheory]
        [InlineData("AccentInactiveBrush")]
        [InlineData("StateOnHoverBrush")]
        public void TheActiveText_OnTheFillOfAnOnState_CanBeRead(string fill)
        {
            Assert.True(ColorMix.Contrast(Token("ActiveForegroundBrush"), Token(fill)) >= 4.5);
        }

        [AvaloniaFact]
        public void Check_AToggleButton_MakesItsTextTheActiveText()
        {
            var toggle = new ToggleButton { Content = "Toggle", Width = 100 };
            Show(toggle);
            Assert.Equal(Token("ForegroundBrush"), TextColorOf(toggle));

            toggle.IsChecked = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Token("ActiveForegroundBrush"), TextColorOf(toggle));
            Assert.Equal(Token("AccentInactiveBrush"), FillOf(toggle, "border"));
        }

        [AvaloniaFact]
        public void Hover_AToggleButtonThatIsOn_HasTheOnHoverFill()
        {
            var toggle = new ToggleButton { Content = "Toggle", Width = 100, IsChecked = true };
            Show(toggle);

            Hover(toggle);

            Assert.Equal(Token("StateOnHoverBrush"), FillOf(toggle, "border"));
            Assert.Equal(Token("ActiveForegroundBrush"), TextColorOf(toggle));
        }

        [AvaloniaFact]
        public void Check_ARadioButton_MakesItsTextTheActiveText()
        {
            var radio = new RadioButton { Content = "Radio", Width = 100 };
            Show(radio);
            Assert.Equal(Token("ForegroundBrush"), TextColorOf(radio));

            radio.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Token("ActiveForegroundBrush"), TextColorOf(radio));

            Hover(radio);
            Assert.Equal(Token("StateOnHoverBrush"), FillOf(radio, "radioButtonBorder"));
        }

        [AvaloniaFact]
        public void Select_ARowOfAList_MakesItsTextTheActiveText()
        {
            var list = new ListBox { ItemsSource = new[] { "First", "Second" }, Width = 140, Height = 80 };
            Show(list);
            list.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            var rows = list.GetVisualDescendants().OfType<ListBoxItem>().ToList();

            Assert.Equal(Token("ActiveForegroundBrush"), TextColorOf(rows[0]));
            Assert.Equal(Token("ForegroundBrush"), TextColorOf(rows[1]));

            Hover(rows[0]);
            Assert.Equal(Token("StateOnHoverBrush"), FillOf(rows[0], "Bd"));
        }

        [AvaloniaFact]
        public void Select_ARowOfAComboBox_MakesItsTextTheActiveText()
        {
            var item = new ComboBoxItem { Content = "Item", Width = 140 };
            Show(item);
            Assert.Equal(Token("ForegroundBrush"), TextColorOf(item));

            item.IsSelected = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Token("ActiveForegroundBrush"), TextColorOf(item));

            Hover(item);
            Assert.Equal(Token("StateOnHoverBrush"), FillOf(item, "border"));
        }

        // The header of an expander is a toggle that is checked while the expander is open. It is not on the accent: its text does not change.
        [AvaloniaFact]
        public void Open_AnExpander_KeepsTheTextOfItsHeader()
        {
            var expander = new Expander { Header = "Header", Content = "Content", IsExpanded = true, Width = 200 };
            Show(expander);

            var header = expander.GetVisualDescendants().OfType<ToggleButton>().First();

            Assert.True(header.IsChecked);
            Assert.Equal(Token("ForegroundBrush"), TextColorOf(header));
        }
    }
}
