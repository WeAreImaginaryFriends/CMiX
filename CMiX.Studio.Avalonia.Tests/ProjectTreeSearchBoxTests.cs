using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.Boolean;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using Composition = CMiX.Core.Compositing.Composition;

namespace CMiX.Studio.Avalonia.Tests
{
    // The search row of the tree: hidden until the search button or Tree.Search shows it, a box for the text, the number of matches,
    // Escape to clear and to hide, Enter and F3 to go from match to match.
    public class ProjectTreeSearchBoxTests : IDisposable
    {
        private TreeTestScreen? screen;

        public void Dispose() => screen?.Dispose();

        // A composition with three layers.
        private (TreeTestScreen Screen, Scene Scene) Show()
        {
            var scene = CreateScene([3]);
            screen = TreeTestScreen.Show(scene.Tree, 400, 700);
            return (screen, scene);
        }

        private static void Pump(TreeTestScreen screen)
        {
            Dispatcher.UIThread.RunJobs();
            screen.Settle();
        }

        private static ButtonIcon SearchButtonOf(ProjectTreeView view) =>
            view.GetVisualDescendants().OfType<ButtonIcon>().First(button => button.Name == "SearchButton");

        private static void Click(Window window, Control control)
        {
            var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        private static void Press(TreeTestScreen screen, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            screen.Window.KeyPressQwerty(key, modifiers);
            Pump(screen);
        }

        // Shows the row with the button and types a text.
        private static void Search(TreeTestScreen screen, string text)
        {
            Click(screen.Window, SearchButtonOf(screen.View));
            Pump(screen);
            screen.View.SearchBox.Text = text;
            Pump(screen);
        }

        // The box has the focus during a pointer drag of a row: Escape ends the drag without a drop, as it does in the tree.
        [AvaloniaFact]
        public void Escape_InTheBoxDuringAPointerDrag_EndsTheDragWithoutADrop()
        {
            var (screen, scene) = Show();
            Search(screen, "Layer");
            var rows = scene.Tree.CompositionNodes()[0].Children[0].Children;
            var before = rows.Select(node => node.Item).ToList();
            Border RowOf(TreeNode node) => TreeTestScreen.RowOf(screen.View, node);
            Point Middle(Border row) => row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), screen.Window)!.Value;

            TreeDrag.Over(screen.Window, Middle(RowOf(rows[0])), Middle(RowOf(rows[2])));
            Assert.True(screen.View.RowDrag.IsDragging);
            screen.View.SearchBox.Focus();
            Pump(screen);

            screen.View.SearchBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Pump(screen);

            Assert.False(screen.View.RowDrag.IsDragging);
            Assert.False(scene.Tree.IsDragging);
            Assert.Equal(before, rows.Select(node => node.Item).ToList());
            Assert.Equal("Layer", scene.Tree.SearchText);
        }

        [AvaloniaFact]
        public void Show_TheTree_HidesTheSearchRow()
        {
            var (screen, _) = Show();

            Assert.False(screen.View.SearchRow.IsVisible);
            Assert.Equal(0, screen.View.SearchRow.Bounds.Height);
        }

        [AvaloniaFact]
        public void Click_TheSearchButton_ShowsTheRowAndFocusesTheBox()
        {
            var (screen, _) = Show();

            Click(screen.Window, SearchButtonOf(screen.View));
            Pump(screen);

            Assert.True(screen.View.SearchRow.IsVisible);
            Assert.True(screen.View.SearchBox.IsFocused);
        }

        [AvaloniaFact]
        public void Type_AText_SetsTheSearchAndShowsTheNumberOfMatches()
        {
            var (screen, scene) = Show();

            Search(screen, "Layer");

            Assert.Equal("Layer", scene.Tree.SearchText);
            Assert.True(scene.Tree.SearchMatchCount > 0);
            Assert.True(screen.View.SearchCount.IsVisible);
            Assert.Equal(scene.Tree.SearchMatchCount.ToString(), screen.View.SearchCount.Text);
        }

        [AvaloniaFact]
        public void Escape_InTheBox_ClearsTheTextThenHidesTheRowAndGivesTheFocusBackToTheTree()
        {
            var (screen, scene) = Show();
            Search(screen, "Layer");

            Press(screen, PhysicalKey.Escape);

            Assert.Equal(string.Empty, scene.Tree.SearchText);
            Assert.True(screen.View.SearchRow.IsVisible);
            Assert.False(screen.View.SearchCount.IsVisible);

            Press(screen, PhysicalKey.Escape);

            Assert.False(screen.View.SearchRow.IsVisible);
            Assert.False(screen.View.SearchBox.IsFocused);
            Assert.True(screen.View.IsKeyboardFocusWithin);
        }

        [AvaloniaFact]
        public void Enter_InTheBox_SelectsTheMatchesInTheOrderOfTheTreeAndGoesRound()
        {
            var (screen, scene) = Show();
            scene.Tree.SelectedNode = null;
            Search(screen, "Layer");
            var matches = scene.Tree.SearchMatches.ToList();
            Assert.True(matches.Count >= 3);

            Press(screen, PhysicalKey.Enter);
            Assert.Same(matches[0], scene.Tree.SelectedNode);

            Press(screen, PhysicalKey.Enter);
            Assert.Same(matches[1], scene.Tree.SelectedNode);

            for (var i = 2; i < matches.Count; i++)
                Press(screen, PhysicalKey.F3);
            Assert.Same(matches[^1], scene.Tree.SelectedNode);

            Press(screen, PhysicalKey.Enter);
            Assert.Same(matches[0], scene.Tree.SelectedNode);
        }

        [AvaloniaFact]
        public void ShiftEnter_InTheBox_SelectsTheMatchBefore()
        {
            var (screen, scene) = Show();
            scene.Tree.SelectedNode = null;
            Search(screen, "Layer");
            var matches = scene.Tree.SearchMatches.ToList();

            Press(screen, PhysicalKey.Enter, RawInputModifiers.Shift);
            Assert.Same(matches[^1], scene.Tree.SelectedNode);

            Press(screen, PhysicalKey.F3, RawInputModifiers.Shift);
            Assert.Same(matches[^2], scene.Tree.SelectedNode);
        }

        [AvaloniaFact]
        public void Enter_OnAMatchUnderACollapsedRow_OpensItsParentsAndSelectsIt()
        {
            var (screen, scene) = Show();
            scene.Tree.SelectedNode = null;
            var layer = scene.LayerNodeOfA(2);
            layer.Parent!.IsExpanded = false;
            Pump(screen);
            Search(screen, layer.Header);
            Assert.Contains(layer, scene.Tree.SearchMatches);

            while (!ReferenceEquals(scene.Tree.SelectedNode, layer))
                Press(screen, PhysicalKey.Enter);

            Assert.True(layer.Parent!.IsExpanded);
            Assert.NotNull(screen.View.RowIndex.FindItem(layer));
        }

        [AvaloniaFact]
        public void Click_TheClearButton_EmptiesTheTextAndKeepsTheRow()
        {
            var (screen, scene) = Show();
            Search(screen, "Layer");

            var clear = screen.View.SearchRow.GetVisualDescendants().OfType<ButtonIcon>().First(button => button.Name == "searchClear");
            Click(screen.Window, clear);
            Pump(screen);

            Assert.Equal(string.Empty, scene.Tree.SearchText);
            Assert.True(screen.View.SearchRow.IsVisible);
        }

        // ---------------------------------------------------------------- no search during a drag

        [AvaloniaFact]
        public void BeginDrag_WithTheSearchShown_TurnsTheCommandOffAndTheBoxTakesNoText()
        {
            var (screen, scene) = Show();
            Search(screen, "Layer");
            var changes = 0;
            scene.Tree.ShowSearchCommand.CanExecuteChanged += (_, _) => changes++;

            scene.Tree.BeginDrag(scene.LayerNodeOfA(0));
            Pump(screen);

            Assert.True(changes > 0);
            Assert.False(scene.Tree.ShowSearchCommand.CanExecute(null));
            Assert.True(screen.View.SearchBox.IsReadOnly);
            screen.View.SearchBox.Focus();
            screen.Window.KeyTextInput("x");
            Pump(screen);
            Assert.Equal("Layer", scene.Tree.SearchText);
            scene.Tree.EndDrag();
        }

        [AvaloniaFact]
        public void Enter_InTheBoxDuringADrag_DoesNotChangeTheSelection()
        {
            var (screen, scene) = Show();
            scene.Tree.SelectedNode = null;
            Search(screen, "Layer");
            scene.Tree.BeginDrag(scene.LayerNodeOfA(0));
            Pump(screen);

            Press(screen, PhysicalKey.Enter);
            scene.Tree.SelectMatch(1);

            Assert.Null(scene.Tree.SelectedNode);
            scene.Tree.EndDrag();
        }

        [AvaloniaFact]
        public void ShowSearch_DuringADrag_DoesNotShowTheRow()
        {
            var (screen, scene) = Show();
            scene.Tree.BeginDrag(scene.LayerNodeOfA(0));
            Pump(screen);

            scene.Tree.ShowSearch();
            Pump(screen);

            Assert.False(screen.View.SearchRow.IsVisible);
            scene.Tree.EndDrag();
        }

        [AvaloniaFact]
        public void EndDrag_AfterADrag_GivesTheSearchBackWithTheSameText()
        {
            var (screen, scene) = Show();
            Search(screen, "Layer");
            scene.Tree.BeginDrag(scene.LayerNodeOfA(0));
            Pump(screen);

            scene.Tree.EndDrag();
            Pump(screen);

            Assert.Equal("Layer", scene.Tree.SearchText);
            Assert.False(screen.View.SearchBox.IsReadOnly);
            screen.View.SearchBox.Focus();
            screen.View.SearchBox.CaretIndex = screen.View.SearchBox.Text!.Length;
            screen.Window.KeyTextInput(" 0");
            Pump(screen);
            Assert.Equal("Layer 0", scene.Tree.SearchText);
        }

        [AvaloniaFact]
        public void Press_CtrlFOverTheTreeDuringADrag_DoesNotShowTheRow()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            TestServiceProviderFactory.Pump();
            window.Focus();
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            window.MouseMove(view.TranslatePoint(new Point(view.Bounds.Width / 2, view.Bounds.Height / 2), window)!.Value);
            Dispatcher.UIThread.RunJobs();
            viewModel.ProjectTree.BeginDrag(viewModel.ProjectTree.CompositionNodes()[0]);

            window.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.False(view.SearchRow.IsVisible);
            viewModel.ProjectTree.EndDrag();
        }

        // ---------------------------------------------------------------- the shortcut of the main window

        [AvaloniaFact]
        public void KeyBindings_OfTheWindow_HoldTheSearchCommandOfTheTreeOnCtrlF()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();

            var binding = window.KeyBindings.Single(candidate => Equals(candidate.Gesture, viewModel.Shortcuts["Tree.Search"]));

            Assert.Equal(new KeyGesture(Key.F, KeyModifiers.Control), viewModel.Shortcuts["Tree.Search"]);
            Assert.Same(viewModel.ProjectTree.ShowSearchCommand, binding.Command);
        }

        [AvaloniaFact]
        public void Press_CtrlFOverTheTree_ShowsTheRowAndFocusesTheBox()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            TestServiceProviderFactory.Pump();
            window.Focus();
            var view = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            window.MouseMove(view.TranslatePoint(new Point(view.Bounds.Width / 2, view.Bounds.Height / 2), window)!.Value);
            Dispatcher.UIThread.RunJobs();

            window.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.True(view.SearchRow.IsVisible);
            Assert.True(view.SearchBox.IsFocused);
        }
    }
}
