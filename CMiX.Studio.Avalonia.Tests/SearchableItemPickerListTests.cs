using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
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

        private static Button[] HighlightedRows(SearchableItemPickerList list) =>
            list.GetVisualDescendants().OfType<Button>().Where(row => row.Classes.Contains("highlighted")).ToArray();

        [AvaloniaFact]
        public void ByDefault_TheFirstRowIsHighlighted()
        {
            var (_, list, _) = Show();

            Assert.Equal(0, list.HighlightedIndex);
            Assert.Equal("Cube", (string)Assert.Single(HighlightedRows(list)).Content!);
        }

        [AvaloniaFact]
        public void Down_MovesTheHighlight_AndEnterPicksThatRow()
        {
            object? picked = null;
            var (_, list, items) = Show(item => picked = item);

            Press(Search(list), Key.Down);
            Press(Search(list), Key.Down);

            Assert.Equal("Sphere", (string)Assert.Single(HighlightedRows(list)).Content!);

            Press(Search(list), Key.Enter);

            Assert.Same(items[2], picked);
        }

        [AvaloniaFact]
        public void Up_MovesTheHighlightBack()
        {
            var (_, list, _) = Show();

            Press(Search(list), Key.Down);
            Press(Search(list), Key.Down);
            Press(Search(list), Key.Up);

            Assert.Equal(1, list.HighlightedIndex);
        }

        [AvaloniaFact]
        public void TheHighlight_StopsAtTheFirstAndTheLastRow()
        {
            var (_, list, _) = Show();

            Press(Search(list), Key.Up);
            Assert.Equal(0, list.HighlightedIndex);

            for (var i = 0; i < 20; i++)
                Press(Search(list), Key.Down);
            Assert.Equal(Names.Length - 1, list.HighlightedIndex);
        }

        [AvaloniaFact]
        public void Down_MovesInsideTheRowsThatMatch()
        {
            object? picked = null;
            var (_, list, items) = Show(item => picked = item);
            Type(list, "c");

            Press(Search(list), Key.Down);
            Press(Search(list), Key.Enter);

            Assert.Equal(new[] { "Cube", "Camera Rig", "Cylinder" }, Rows(list));
            Assert.Same(items[1], picked);
        }

        [AvaloniaFact]
        public void TypingAgain_PutsTheHighlightOnTheFirstRow()
        {
            var (_, list, _) = Show();
            Press(Search(list), Key.Down);
            Press(Search(list), Key.Down);

            Type(list, "c");

            Assert.Equal(0, list.HighlightedIndex);
            Assert.Equal("Cube", (string)Assert.Single(HighlightedRows(list)).Content!);
        }

        [AvaloniaFact]
        public void WithoutAMatch_ThereIsNoHighlight_AndTheKeysDoNothing()
        {
            object? picked = null;
            var (_, list, _) = Show(item => picked = item);
            Type(list, "zzz");

            Press(Search(list), Key.Down);
            Press(Search(list), Key.Enter);

            Assert.Equal(-1, list.HighlightedIndex);
            Assert.Empty(HighlightedRows(list));
            Assert.Null(picked);
        }

        [AvaloniaFact]
        public void TheHighlightedRow_IsScrolledIntoView()
        {
            var items = new ObservableCollection<Item>(Enumerable.Range(1, 40).Select(i => new Item($"Entity {i}")));
            var list = new SearchableItemPickerList { ItemsSource = items, TextPath = nameof(Item.Name), Width = 200 };
            new Window { Content = list, Width = 400, Height = 800 }.Show();
            Dispatcher.UIThread.RunJobs();

            for (var i = 0; i < 15; i++)
                Press(Search(list), Key.Down);
            Dispatcher.UIThread.RunJobs();

            var scroll = list.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "PART_Scroll");
            var row = Assert.Single(HighlightedRows(list));
            var top = row.TranslatePoint(new Point(0, 0), scroll)!.Value.Y;

            Assert.Equal("Entity 16", (string)row.Content!);
            Assert.True(top >= -0.5 && top + row.Bounds.Height <= scroll.Viewport.Height + 0.5, $"The row is at {top} in a viewport of {scroll.Viewport.Height}.");
        }

        [AvaloniaFact]
        public void TheSearchIcon_IsInsideTheField_AndTheTextStartsAfterIt()
        {
            var (_, list, _) = Show();
            var box = Search(list);
            var icon = list.GetVisualDescendants().OfType<CMiX.UI.Controls.AppIcon>().Single(i => i.Name == "PART_SearchIcon");

            var origin = icon.TranslatePoint(new Point(0, 0), box)!.Value;

            Assert.Equal("Search", icon.IconKey);
            Assert.True(origin.X >= 0 && origin.X + icon.Bounds.Width <= box.Bounds.Width);
            Assert.True(origin.Y >= 0 && origin.Y + icon.Bounds.Height <= box.Bounds.Height);
            Assert.True(box.Padding.Left >= origin.X + icon.Bounds.Width, "The text would start under the icon.");
            Assert.False(icon.IsHitTestVisible);
        }

        // The keys go through the window, so the text box sees them as a user's keys, with its own handling first.
        [AvaloniaFact]
        public void RealKeys_AfterTypingText_MoveTheHighlightBothWays()
        {
            object? picked = null;
            var (window, list, items) = Show(item => picked = item);
            window.KeyTextInput("c");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new[] { "Cube", "Camera Rig", "Cylinder" }, Rows(list));

            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            Assert.Equal(1, list.HighlightedIndex);

            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            Assert.Equal(2, list.HighlightedIndex);

            window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
            Assert.Equal(1, list.HighlightedIndex);

            window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
            Assert.Equal(0, list.HighlightedIndex);

            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Same(items[1], picked);
        }

        [AvaloniaFact]
        public void RealKeys_AfterTypingLongerText_StillMoveTheHighlight()
        {
            var (window, list, _) = Show();
            window.KeyTextInput("cy");
            Dispatcher.UIThread.RunJobs();

            window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);

            Assert.Equal(0, list.HighlightedIndex);
            Assert.Equal(new[] { "Cylinder" }, Rows(list));
        }

        [AvaloniaFact]
        public void RealKeys_UpAndDown_DoNotMoveTheCaretOfTheField()
        {
            var (window, list, _) = Show();
            window.KeyTextInput("ca");
            Dispatcher.UIThread.RunJobs();
            var caret = Search(list).CaretIndex;

            window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
            window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);

            Assert.Equal(caret, Search(list).CaretIndex);
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
