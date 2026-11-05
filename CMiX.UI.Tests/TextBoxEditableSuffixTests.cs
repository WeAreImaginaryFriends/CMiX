using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls.String;
using Xunit;

namespace CMiX.UI.Tests
{
    // The suffix is a control that follows the text of the box, such as the counts of a row of a tree.
    public class TextBoxEditableSuffixTests
    {
        private static (TextBoxEditable Box, Border Suffix, Window Window) Show(string text = "Layer 1")
        {
            var suffix = new Border { Width = 20, Height = 10 };
            var box = new TextBoxEditable { Text = text, Suffix = suffix };
            var window = new Window { Content = box, Width = 300, Height = 60 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (box, suffix, window);
        }

        [AvaloniaFact]
        public void TheSuffix_FollowsTheEndOfTheText_AtAFixedDistance()
        {
            var (box, suffix, window) = Show();
            var text = box.GetVisualDescendants().OfType<TextBlock>().First(block => block.Name == "TextDisplay");

            var textRight = text.TranslatePoint(new Point(text.Bounds.Width, 0), window)!.Value.X;
            var suffixLeft = suffix.TranslatePoint(new Point(0, 0), window)!.Value.X;

            Assert.Equal(4, suffixLeft - textRight, 1);
        }

        [AvaloniaFact]
        public void ALongerText_MovesTheSuffix()
        {
            var (_, shortSuffix, window) = Show("A");
            var shortLeft = shortSuffix.TranslatePoint(new Point(0, 0), window)!.Value.X;

            var (_, longSuffix, longWindow) = Show("A much longer name");
            var longLeft = longSuffix.TranslatePoint(new Point(0, 0), longWindow)!.Value.X;

            Assert.True(longLeft > shortLeft);
        }

        [AvaloniaFact]
        public void TheSuffix_IsCenteredAcrossTheHeightOfTheBox()
        {
            var (box, suffix, window) = Show();

            var middle = suffix.TranslatePoint(new Point(0, suffix.Bounds.Height / 2), box)!.Value.Y;

            Assert.Equal(box.Bounds.Height / 2, middle, 1);
        }

        [AvaloniaFact]
        public void WhileTheTextIsEdited_TheSuffixIsHidden()
        {
            var (box, suffix, _) = Show();
            Assert.True(suffix.IsEffectivelyVisible);

            box.IsEditing = true;
            Dispatcher.UIThread.RunJobs();

            Assert.False(suffix.IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void ABoxWithoutASuffix_HasTheSameTextPlace()
        {
            var (_, _, window) = Show();
            var withSuffix = window.GetVisualDescendants().OfType<TextBlock>().First(block => block.Name == "TextDisplay").Bounds;

            var plain = new TextBoxEditable { Text = "Layer 1" };
            var plainWindow = new Window { Content = plain, Width = 300, Height = 60 };
            plainWindow.Show();
            Dispatcher.UIThread.RunJobs();
            var withoutSuffix = plain.GetVisualDescendants().OfType<TextBlock>().First(block => block.Name == "TextDisplay").Bounds;

            Assert.Equal(withoutSuffix, withSuffix);
        }
    }
}
