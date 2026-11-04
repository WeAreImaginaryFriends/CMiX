using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls;
using CMiX.UI.Controls.Collection;
using Xunit;

namespace CMiX.UI.Tests
{
    // A row of the list can have the icon of its item before its text.
    public class ItemPickerListIconTests
    {
        private sealed record Item(string Name, string Icon);

        private static (Window Window, ItemPickerList List) Show(string iconPath)
        {
            var list = new ItemPickerList
            {
                ItemsSource = new[] { new Item("Tree", "Tree"), new Item("Assets", "Resources") },
                TextPath = nameof(Item.Name),
                IconPath = iconPath,
                Width = 200
            };
            var window = new Window { Content = list, Width = 300, Height = 300 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, list);
        }

        [AvaloniaFact]
        public void WithAnIconPath_EachRowHasTheIconAndTheText()
        {
            var (_, list) = Show(nameof(Item.Icon));

            var rows = list.GetVisualDescendants().OfType<Button>().ToList();

            Assert.Equal(new[] { "Tree", "Resources" }, rows.Select(row => row.GetVisualDescendants().OfType<AppIcon>().Single().IconKey));
            Assert.Equal(new[] { "Tree", "Assets" }, rows.Select(row => row.GetVisualDescendants().OfType<TextBlock>().Single().Text));
        }

        [AvaloniaFact]
        public void WithoutAnIconPath_TheRowsAreTheTextOnly_AsBefore()
        {
            var (_, list) = Show(string.Empty);

            var rows = list.GetVisualDescendants().OfType<Button>().ToList();

            Assert.Equal(new[] { "Tree", "Assets" }, rows.Select(row => (string)row.Content!));
            Assert.All(rows, row => Assert.Empty(row.GetVisualDescendants().OfType<AppIcon>()));
        }

        [AvaloniaFact]
        public void TheIconRows_KeepTheSizeOfTheOtherRows()
        {
            var (_, withIcons) = Show(nameof(Item.Icon));
            var (_, textOnly) = Show(string.Empty);

            Assert.Equal(
                textOnly.GetVisualDescendants().OfType<Button>().First().Bounds.Height,
                withIcons.GetVisualDescendants().OfType<Button>().First().Bounds.Height);
        }
    }
}
