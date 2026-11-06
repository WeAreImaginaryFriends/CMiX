using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls.Boolean;
using CMiX.UI.Tests.Support;
using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace CMiX.UI.Tests
{
    // ButtonMomentary is a real Button. Its Value is true only while the pointer holds it down.
    public class ButtonMomentaryTests
    {
        private static Window Show(Control content)
        {
            var window = new Window { Content = content, Width = 300, Height = 100 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return window;
        }

        [AvaloniaFact]
        public void Create_TheMomentaryButton_IsAButton()
        {
            Assert.IsAssignableFrom<Button>(new ButtonMomentary());
        }

        [AvaloniaFact]
        public void Press_SetsTheValueTrue_AndReleaseSetsItFalse()
        {
            var button = new ButtonMomentary();
            var window = Show(button);

            PointerInput.Press(window, button, MouseButton.Left);
            Assert.True(button.Value);

            PointerInput.Release(window, button, MouseButton.Left);
            Assert.False(button.Value);
        }

        [AvaloniaFact]
        public void Value_SetFromOutside_ShowsTheHeldState()
        {
            var button = new ButtonMomentary();
            Show(button);
            var border = button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "border");
            Color Current() => ((ISolidColorBrush)border.Background!).Color;

            button.Value = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Tokens.Get<ISolidColorBrush>("AccentBrush").Color, Current());

            button.Value = false;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Tokens.Get<ISolidColorBrush>("StateIdleBrush").Color, Current());
        }

        [AvaloniaFact]
        public void Button_WithoutResetCommand_HasNoMenu()
        {
            var button = new ButtonMomentary();
            Show(button);

            Assert.Null(button.ContextMenu);
        }

        [AvaloniaFact]
        public void Button_WithResetCommand_HasAResetMenuThatRunsIt()
        {
            var ran = false;
            var button = new ButtonMomentary { ResetCommand = new RelayCommand(() => ran = true) };
            Show(button);

            button.ContextMenu!.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Reset")).Command!.Execute(null);

            Assert.True(ran);
        }
    }
}
