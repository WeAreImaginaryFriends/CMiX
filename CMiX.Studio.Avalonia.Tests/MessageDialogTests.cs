using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Services;
using CMiX.Studio.Avalonia.Views.Dialogs;
using CMiX.UI.Controls;
using CMiX.UI.Controls.Parts;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The one message window of the app: the frame of the menus, an icon, a text and a row of buttons.
    public class MessageDialogTests
    {
        private static readonly MessageDialogOptions ThreeButtons = new(
            "Three buttons.",
            "Info",
            new[]
            {
                new MessageDialogButton("Save", MessageDialogResult.Save, IsDefault: true),
                new MessageDialogButton("Don't Save", MessageDialogResult.DontSave),
                new MessageDialogButton("Cancel", MessageDialogResult.Cancel, IsCancel: true)
            });

        private static Window Owner()
        {
            var owner = new Window { Width = 600, Height = 400 };
            owner.Show();
            Dispatcher.UIThread.RunJobs();
            return owner;
        }

        private static (MessageDialog Dialog, Task<MessageDialogResult> Result) Show(MessageDialogOptions options)
        {
            var dialog = new MessageDialog(options);
            var result = dialog.ShowDialog<MessageDialogResult>(Owner());
            Dispatcher.UIThread.RunJobs();
            return (dialog, result);
        }

        private static List<Button> Buttons(Control dialog) => dialog.GetVisualDescendants().OfType<Button>().ToList();

        private static void Press(MessageDialog dialog, Key key)
        {
            dialog.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = dialog });
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaFact]
        public void Show_WithOneButton_HasTheTextAndTheButton()
        {
            var (dialog, _) = Show(MessageDialogOptions.Notice("The file is broken.", "Warning"));

            var texts = dialog.GetVisualDescendants().OfType<TextBlock>().Where(text => text.IsVisible).Select(text => text.Text);
            var button = Assert.Single(Buttons(dialog));

            Assert.Contains("The file is broken.", texts);
            Assert.Equal("OK", button.Content);
            Assert.Equal("OkButton", button.Name);
            Assert.Equal("Warning", dialog.GetVisualDescendants().OfType<AppIcon>().Single().IconKey);
        }

        [AvaloniaFact]
        public void Show_WithATitle_ShowsTheTitleAboveTheText()
        {
            var (dialog, _) = Show(MessageNotice.Options("cmix.cmix: The file is broken.", "Open Project"));

            TextBlock Named(string name) => dialog.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == name);
            var title = Named("TitleText");
            var message = Named("MessageText");

            Assert.True(title.IsVisible);
            Assert.Equal("Open Project", title.Text);
            Assert.Equal("cmix.cmix: The file is broken.", message.Text);
            Assert.True(title.TranslatePoint(default, dialog)!.Value.Y < message.TranslatePoint(default, dialog)!.Value.Y);
            Assert.Equal("Error", dialog.GetVisualDescendants().OfType<AppIcon>().Single().IconKey);
        }

        [AvaloniaFact]
        public async Task Enter_RunsTheDefaultButton()
        {
            var (dialog, result) = Show(ThreeButtons);

            Press(dialog, Key.Enter);

            Assert.Equal(MessageDialogResult.Save, await result);
        }

        [AvaloniaFact]
        public async Task Escape_RunsTheCancelButton()
        {
            var (dialog, result) = Show(MessageDialogOptions.Notice("Text.", "Warning"));

            Press(dialog, Key.Escape);

            Assert.Equal(MessageDialogResult.Ok, await result);
        }

        [AvaloniaTheory]
        [InlineData(0, MessageDialogResult.Save)]
        [InlineData(1, MessageDialogResult.DontSave)]
        [InlineData(2, MessageDialogResult.Cancel)]
        public async Task Buttons_ThreeButtons_GiveTheirResults(int index, MessageDialogResult expected)
        {
            var (dialog, result) = Show(ThreeButtons);

            Buttons(dialog)[index].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(expected, await result);
        }

        [AvaloniaFact]
        public void Frame_IsThePopupFrame()
        {
            var (dialog, _) = Show(ThreeButtons);

            var frame = Assert.Single(dialog.GetVisualDescendants().OfType<PopupFrame>());

            Assert.Equal(380, frame.PanelMinWidth);
            Assert.True(frame.Bounds.Width >= 380, $"The frame is {frame.Bounds.Width} wide.");
        }
    }
}
