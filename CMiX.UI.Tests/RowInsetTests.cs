using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CMiX.UI.Tests
{
    // Every row starts its content 8 from its left edge and has the round corners of the theme: a list row, a combo row and a menu row.
    public class RowInsetTests
    {
        private static T Show<T>(T control) where T : Control
        {
            new Window { Content = control, Width = 300, Height = 200 }.Show();
            Dispatcher.UIThread.RunJobs();
            return control;
        }

        private static CornerRadius Round => (CornerRadius)Application.Current!.FindResource("CornerRadiusAll")!;

        [AvaloniaFact]
        public void Show_ARowOfAList_StartsItsContentAtEight()
        {
            var list = Show(new ListBox { ItemsSource = new[] { "Row" }, Width = 140, Height = 40 });

            var row = list.GetVisualDescendants().OfType<ListBoxItem>().First();
            var border = row.GetVisualDescendants().OfType<Border>().First(part => part.Name == "Bd");

            Assert.Equal(8, border.Padding.Left);
            Assert.Equal(Round, border.CornerRadius);
        }

        [AvaloniaFact]
        public void Show_ARowOfAComboBox_StartsItsContentAtEight()
        {
            var item = Show(new ComboBoxItem { Content = "Item", Width = 140 });

            var presenter = item.GetVisualDescendants().OfType<ContentPresenter>().First(part => part.Name == "PART_ContentPresenter");
            var border = item.GetVisualDescendants().OfType<Border>().First(part => part.Name == "border");

            Assert.Equal(8, presenter.Margin.Left);
            Assert.Equal(Round, border.CornerRadius);
        }

        [AvaloniaFact]
        public void Show_ARowOfAMenu_StartsItsContentAtEight()
        {
            var row = Show(new MenuItem { Header = "Duplicate", Theme = (ControlTheme)Application.Current!.FindResource("MenuItemContext")!, Width = 140 });

            var root = row.GetVisualDescendants().OfType<Border>().First(part => part.Name == "root");

            Assert.Equal(8, root.Padding.Left);
            Assert.Equal(Round, root.CornerRadius);
        }
    }
}
