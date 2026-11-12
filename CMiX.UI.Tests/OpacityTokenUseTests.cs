using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CMiX.UI.Tests
{
    // The styles take their opacities from the tokens: a disabled control has OpacityDisabled, a dim icon has OpacityIconIdle.
    public class OpacityTokenUseTests
    {
        private static T Show<T>(T control) where T : Control
        {
            new Window { Content = control, Width = 300, Height = 200 }.Show();
            Dispatcher.UIThread.RunJobs();
            return control;
        }

        private static T Part<T>(Visual root, string name) where T : Control =>
            root.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

        [AvaloniaTheory]
        [InlineData(null)]
        [InlineData("TextBoxNumbersOnly")]
        public void TextBox_Disabled_HasTheDisabledOpacity(string? themeKey)
        {
            var box = new TextBox { IsEnabled = false };
            if (themeKey != null)
                box.Theme = (ControlTheme)Application.Current!.FindResource(themeKey)!;
            Show(box);

            Assert.Equal(Tokens.Get<double>("OpacityDisabled"), Part<Border>(box, "border").Opacity);
            Assert.Equal(0.5, Part<Border>(box, "border").Opacity);
        }

        [AvaloniaFact]
        public void TabItem_Disabled_HasTheDisabledOpacity()
        {
            var theme = (ControlTheme)Application.Current!.FindResource("TabItemCMiX")!;
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Theme = theme, Header = "A" });
            tabs.Items.Add(new TabItem { Theme = theme, Header = "B", IsEnabled = false });
            Show(tabs);
            var off = tabs.GetVisualDescendants().OfType<TabItem>().ToList()[1];

            Assert.Equal(0.5, Part<ContentPresenter>(off, "PART_ContentPresenter").Opacity);
        }

        [AvaloniaFact]
        public void TabItemHorizontal_NotSelected_HasTheIdleIconOpacity()
        {
            var theme = (ControlTheme)Application.Current!.FindResource("TabItemHorizontal")!;
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Theme = theme, Header = "A" });
            tabs.Items.Add(new TabItem { Theme = theme, Header = "B" });
            Show(tabs);
            var idle = tabs.GetVisualDescendants().OfType<TabItem>().ToList()[1];

            Assert.Equal(Tokens.Get<double>("OpacityIconIdle"), Part<ContentPresenter>(idle, "PART_ContentPresenter").Opacity);
        }

        [AvaloniaFact]
        public void ButtonIconClose_AtRest_HasTheIdleIconOpacity()
        {
            var theme = (ControlTheme)Application.Current!.FindResource("ButtonIconClose")!;
            var button = Show(new Button { Theme = theme, Content = "Close" });

            Assert.Equal(0.6, Part<CMiX.UI.Controls.AppIcon>(button, "image").Opacity);
        }

        [AvaloniaFact]
        public void ToggleButtonIcon_AtRest_HasTheIdleIconOpacity()
        {
            var theme = (ControlTheme)Application.Current!.FindResource("ToggleButtonIcon")!;
            var toggle = Show(new ToggleButton { Theme = theme });

            Assert.Equal(0.6, Part<Border>(toggle, "border").Opacity);
        }

        [AvaloniaFact]
        public void TextBlock_Disabled_HasTheDisabledOpacity()
        {
            var text = Show(new TextBlock { Text = "A", IsEnabled = false });

            Assert.Equal(0.5, text.Opacity);
        }
    }
}
