using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using CMiX.Core.Compositing;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The undo and redo keys of the main window. The gestures come from the shortcut map.
    public class MainWindowShortcutTests
    {
        private static void Press(Views.MainWindow window, PhysicalKey key, RawInputModifiers modifiers)
        {
            window.KeyPressQwerty(key, modifiers);
            TestServiceProviderFactory.Pump();
        }

        [AvaloniaFact]
        public void Press_CtrlZThenCtrlShiftZ_UndoesAndRedoesTheStep()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            var items = viewModel.Project.CompositionManager.ManagerData.Items;
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            TestServiceProviderFactory.Pump();
            window.Focus();
            Assert.Single(items);

            Press(window, PhysicalKey.Z, RawInputModifiers.Control);
            Assert.Empty(items);

            Press(window, PhysicalKey.Z, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Single(items);
        }

        [AvaloniaFact]
        public void KeyBindings_OfTheWindow_FollowTheMap()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            var undo = window.KeyBindings.Single(binding => Equals(binding.Gesture, new KeyGesture(Key.Z, KeyModifiers.Control)));
            var gesture = new KeyGesture(Key.U, KeyModifiers.Control | KeyModifiers.Alt);

            viewModel.Shortcuts.Set("Edit.Undo", gesture);
            TestServiceProviderFactory.Pump();

            Assert.Equal(gesture, undo.Gesture);
        }

        [AvaloniaFact]
        public void KeyBindings_OfTheWindow_HoldTheUndoAndRedoCommands()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();

            KeyBinding For(string name) => window.KeyBindings.Single(binding => Equals(binding.Gesture, viewModel.Shortcuts[name]));

            Assert.Same(viewModel.UndoCommand, For("Edit.Undo").Command);
            Assert.Same(viewModel.RedoCommand, For("Edit.Redo").Command);
            Assert.Same(viewModel.RedoCommand, For("Edit.RedoAlt").Command);
        }
    }
}
