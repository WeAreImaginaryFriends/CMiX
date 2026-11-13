using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Popups;
using Xunit;

namespace CMiX.UI.Tests
{
    // A tooltip is a card: the color of the token PopupBackgroundBrush, with no outline. A string shows as a title, and a
    // TipContent shows its lines.
    public class ToolTipLookTests
    {
        private static ToolTip Show(object content)
        {
            var tip = new ToolTip { Content = content };
            new Window { Content = tip, Width = 300, Height = 200 }.Show();
            Dispatcher.UIThread.RunJobs();
            return tip;
        }

        private static string[] Texts(ToolTip tip) =>
            tip.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsEffectivelyVisible).Select(text => text.Text ?? string.Empty).ToArray();

        [AvaloniaFact]
        public void Show_ATooltip_HasTheLookOfTheCard()
        {
            var tip = Show("Tip");

            var card = tip.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_Card");

            var fill = ((ISolidColorBrush)Application.Current!.FindResource("PopupBackgroundBrush")!).Color;
            Assert.Equal(fill, ((ISolidColorBrush)card.Background!).Color);
            Assert.Equal(new Thickness(0), card.BorderThickness);
        }

        [AvaloniaFact]
        public void Show_AString_ShowsAsAMediumTitle()
        {
            var tip = Show("Add");

            var title = tip.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Add");
            Assert.Equal(FontWeight.Medium, title.FontWeight);
            Assert.Equal(new[] { "Add" }, Texts(tip));
        }

        [AvaloniaFact]
        public void Show_ATipContent_ShowsOnlyTheLinesThatItHas()
        {
            var tip = Show(new TipContent("Add", "Shift+A", "Adds under the row."));

            Assert.Equal(new[] { "Add", "Shift+A", "Adds under the row." }, Texts(tip));
        }

        [AvaloniaFact]
        public void ATipContent_WithAllTheLines_ShowsThemInOrder()
        {
            var tip = Show(new TipContent("Remove", "Del", "Takes the row out.", "Select a row first.", "Tree.Remove"));

            Assert.Equal(new[] { "Remove", "Del", "Takes the row out.", "Select a row first.", "Tree.Remove" }, Texts(tip));
        }

        [AvaloniaFact]
        public void Show_TheReasonLine_HasTheWarningColor()
        {
            var tip = Show(new TipContent("Remove", Reason: "Select a row first."));

            var reason = tip.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Select a row first.");
            var warning = ((ISolidColorBrush)Application.Current!.FindResource("PopupCardWarningBrush")!).Color;
            Assert.Equal(warning, ((ISolidColorBrush)reason.Foreground!).Color);
        }

        [AvaloniaFact]
        public void Show_TheShortcutAndTheFooter_UseTheMonoFont()
        {
            var tip = Show(new TipContent("Add", "Shift+A", Footer: "Tree.Add"));

            var mono = (FontFamily)Application.Current!.FindResource("FontFamilyMono")!;
            Assert.All(new[] { "Shift+A", "Tree.Add" }, text =>
                Assert.Equal(mono, tip.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == text).FontFamily));
        }

        // The last line of a tooltip is quiet, and it can still be read: TextMutedBrush has a contrast of 4.5:1 or more on the card.
        [AvaloniaFact]
        public void Show_TheFooter_HasTheMutedTextColorThatCanBeRead()
        {
            var tip = Show(new TipContent("Add", "Shift+A", Footer: "Tree.Add"));

            var footer = tip.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == "Tree.Add");
            var muted = ((ISolidColorBrush)Application.Current!.FindResource("TextMutedBrush")!).Color;
            var card = ((ISolidColorBrush)Application.Current!.FindResource("PopupBackgroundBrush")!).Color;

            Assert.Equal(muted, ((ISolidColorBrush)footer.Foreground!).Color);
            Assert.True(CMiX.UI.Mathematics.ColorMix.Contrast(muted, card) >= 4.5);
        }
    }
}
