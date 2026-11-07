using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using CMiX.Core.Compositing;
using CMiX.Studio.Avalonia.Views;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

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

        [AvaloniaFact]
        public void KeyBindings_OfTheWindow_HoldTheAddCommandOfTheTree()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();

            var add = window.KeyBindings.Single(binding => Equals(binding.Gesture, viewModel.Shortcuts["Tree.Add"]));

            Assert.Same(viewModel.ProjectTree.AddSelectedCommand, add.Command);
        }

        [AvaloniaFact]
        public void Press_ShiftAWithTheFocusOutsideTheTree_OpensTheAddPanelOfTheSelectedRow()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();
            TestServiceProviderFactory.ExpandAll(viewModel.ProjectTree);
            TestServiceProviderFactory.Pump();
            var layer = viewModel.ProjectTree.Roots[0].Children[0];
            viewModel.ProjectTree.SelectedNode = layer;
            TestServiceProviderFactory.Pump();
            window.Focus();

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);

            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            Assert.True(view.RowAddPanel.IsOpen);
            Assert.Equal(new[] { "Layer", "Content", "Texture", "Filter", "Modifier", "Modulator", "Project" }, view.RowAddPanel.List.Groups!.Select(group => group.Header));
            view.RowAddPanel.Close();
        }

        [AvaloniaFact]
        public void Press_ShiftAWithNothingSelected_AddsACompositionAtOnce()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            TestServiceProviderFactory.Pump();
            viewModel.ProjectTree.SelectedNode = null;
            var before = viewModel.Project.CompositionManager.ManagerData.Items.Count;
            window.Focus();

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);

            Assert.Equal(before + 1, viewModel.Project.CompositionManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void Set_AnotherGestureForTreeAdd_ReplacesShiftA()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            TestServiceProviderFactory.Pump();
            viewModel.ProjectTree.SelectedNode = viewModel.ProjectTree.Roots[0];
            viewModel.Shortcuts.Set("Tree.Add", new KeyGesture(Key.N, KeyModifiers.Control));
            TestServiceProviderFactory.Pump();
            window.Focus();
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);
            Assert.False(view.RowAddPanel.IsOpen);

            Press(window, PhysicalKey.N, RawInputModifiers.Control);
            Assert.True(view.RowAddPanel.IsOpen);
            view.RowAddPanel.Close();
        }
    }
}
