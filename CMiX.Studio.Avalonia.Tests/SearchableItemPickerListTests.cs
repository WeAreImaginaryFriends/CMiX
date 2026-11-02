using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls.Collection;
using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The list with a search field. The rows are the items whose text holds the text of the field.
    public class SearchableItemPickerListTests
    {
        private sealed record Item(string Name);

        private static readonly string[] Names = { "Cube", "Camera Rig", "Sphere", "Cylinder", "Plane" };

        private static (Window Window, SearchableItemPickerList List, ObservableCollection<Item> Items) Show(Action<object?>? onPick = null)
        {
            var items = new ObservableCollection<Item>(Names.Select(name => new Item(name)));
            var list = new SearchableItemPickerList
            {
                ItemsSource = items,
                TextPath = nameof(Item.Name),
                ItemCommand = onPick == null ? null : new RelayCommand<object>(item => onPick(item)),
                Width = 200
            };
            var window = new Window { Content = list, Width = 400, Height = 600 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, list, items);
        }

        private static TextBox Search(SearchableItemPickerList list) =>
            list.GetVisualDescendants().OfType<TextBox>().Single(box => box.Name == "PART_Search");

        private static TextBlock NoMatch(SearchableItemPickerList list) =>
            list.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "PART_NoMatch");

        private static string[] Rows(SearchableItemPickerList list) =>
            list.GetVisualDescendants().OfType<Button>().Select(row => (string)row.Content!).ToArray();

        private static void Type(SearchableItemPickerList list, string text)
        {
            Search(list).Text = text;
            Dispatcher.UIThread.RunJobs();
        }

        private static void Press(TextBox box, Key key)
        {
            box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaFact]
        public void WithoutText_ShowsEveryItem()
        {
            var (_, list, _) = Show();

            Assert.Equal(Names, Rows(list));
            Assert.False(NoMatch(list).IsVisible);
        }

        [AvaloniaFact]
        public void TypedText_KeepsTheItemsThatHoldIt_InTheOrderOfTheSource()
        {
            var (_, list, _) = Show();

            Type(list, "c");

            Assert.Equal(new[] { "Cube", "Camera Rig", "Cylinder" }, Rows(list));
        }

        [AvaloniaFact]
        public void TheSearch_IgnoresTheCase()
        {
            var (_, list, _) = Show();

            Type(list, "CAM");

            Assert.Equal(new[] { "Camera Rig" }, Rows(list));
        }

        [AvaloniaFact]
        public void TextThatNothingHolds_ShowsTheNoMatchLine()
        {
            var (_, list, _) = Show();

            Type(list, "zzz");

            Assert.Empty(Rows(list));
            Assert.True(NoMatch(list).IsVisible);
        }

        [AvaloniaFact]
        public void AnEmptySource_DoesNotShowTheNoMatchLine()
        {
            var (_, list, items) = Show();

            items.Clear();
            Dispatcher.UIThread.RunJobs();

            Assert.False(NoMatch(list).IsVisible);
        }

        [AvaloniaFact]
        public void AnItemAdded_ThatMatches_AppearsAndOneThatDoesNot_Stays_Out()
        {
            var (_, list, items) = Show();
            Type(list, "cube");

            items.Add(new Item("Cube 2"));
            items.Add(new Item("Torus"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new[] { "Cube", "Cube 2" }, Rows(list));
        }

        [AvaloniaFact]
        public void Enter_PicksTheFirstRow()
        {
            object? picked = null;
            var (_, list, items) = Show(item => picked = item);
            Type(list, "cy");

            Press(Search(list), Key.Enter);

            Assert.Same(items[3], picked);
        }

        [AvaloniaFact]
        public void Enter_WithoutAMatch_PicksNothing()
        {
            object? picked = null;
            var (_, list, _) = Show(item => picked = item);
            Type(list, "zzz");

            Press(Search(list), Key.Enter);

            Assert.Null(picked);
        }

        [AvaloniaFact]
        public void Escape_ClearsTheText()
        {
            var (_, list, _) = Show();
            Type(list, "cu");

            Press(Search(list), Key.Escape);

            Assert.Equal(string.Empty, Search(list).Text);
            Assert.Equal(Names, Rows(list));
        }

        [AvaloniaFact]
        public void AppearingAgain_StartsWithoutAFilter()
        {
            var (window, list, _) = Show();
            Type(list, "cu");

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.Content = list;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(string.Empty, Search(list).Text);
            Assert.Equal(Names, Rows(list));
        }

        [AvaloniaFact]
        public void TheFieldTakesTheFocus_WhenTheListAppears()
        {
            var (_, list, _) = Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(Search(list).IsFocused);
        }
    }
}
