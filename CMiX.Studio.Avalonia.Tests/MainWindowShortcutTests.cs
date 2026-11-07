using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
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

        // The pointer on the middle of the tree view, or beside it.
        private static void PointOnTree(Window window)
        {
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            window.MouseMove(view.TranslatePoint(new Point(view.Bounds.Width / 2, view.Bounds.Height / 2), window)!.Value);
            Dispatcher.UIThread.RunJobs();
        }

        private static void PointBesideTree(Window window)
        {
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var right = view.TranslatePoint(new Point(view.Bounds.Width, 0), window)!.Value.X;
            window.MouseMove(new Point(right + 10, 10));
            Dispatcher.UIThread.RunJobs();
            Assert.False(view.IsPointerOver);
        }

        [AvaloniaFact]
        public void KeyBindings_OfTheWindow_HoldTheAddCommandOfTheTree()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();

            var add = window.KeyBindings.Single(binding => Equals(binding.Gesture, viewModel.Shortcuts["Tree.Add"]));

            Assert.Same(viewModel.ProjectTree.AddUnderPointerCommand, add.Command);
        }

        [AvaloniaFact]
        public void Press_ShiftAWithTheFocusOutsideTheTree_OpensTheMenuWithTheAddPanelOpenAndExpanded()
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
            PointOnTree(window);

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);

            // The menu of the row opens with its Add panel, not the panel of the + of the row.
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            TestServiceProviderFactory.Pump();
            Assert.False(view.RowAddPanel.IsOpen);
            Assert.True(view.MenuAddPanel.IsOpen);
            var groups = view.MenuAddPanel.List.Groups!.ToList();
            Assert.Equal("Project", groups[^1].Header);
            Assert.All(groups, group => Assert.True(view.MenuAddPanel.List.IsOpen(group)));
            view.MenuAddPanel.Close();
        }

        [AvaloniaFact]
        public void Press_ShiftAWithThePointerBesideTheTree_DoesNothing()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            TestServiceProviderFactory.Pump();
            viewModel.ProjectTree.SelectedNode = null;
            var before = viewModel.Project.CompositionManager.ManagerData.Items.Count;
            window.Focus();
            PointBesideTree(window);

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);

            Assert.Equal(before, viewModel.Project.CompositionManager.ManagerData.Items.Count);
            Assert.False(window.GetVisualDescendants().OfType<ProjectTreeView>().Single().MenuAddPanel.IsOpen);
        }

        [AvaloniaFact]
        public void Press_ShiftAOverAnEmptyTree_OpensThePanelOfTheProjectAndAddsNothing()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            TestServiceProviderFactory.Pump();
            viewModel.ProjectTree.SelectedNode = null;
            var before = viewModel.Project.CompositionManager.ManagerData.Items.Count;
            window.Focus();
            PointOnTree(window);

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);
            TestServiceProviderFactory.Pump();

            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            Assert.Equal(before, viewModel.Project.CompositionManager.ManagerData.Items.Count);
            Assert.True(view.MenuAddPanel.IsOpen);
            Assert.Equal(new[] { "Project" }, view.MenuAddPanel.List.Groups!.Select(group => group.Header));
            view.MenuAddPanel.Close();
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
            PointOnTree(window);
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();

            Press(window, PhysicalKey.A, RawInputModifiers.Shift);
            TestServiceProviderFactory.Pump();
            Assert.False(view.MenuAddPanel.IsOpen);

            Press(window, PhysicalKey.N, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();
            Assert.True(view.MenuAddPanel.IsOpen);
            view.MenuAddPanel.Close();
        }
    }
}
