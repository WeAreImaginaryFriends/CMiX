using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeTestScreen;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // Ctrl+Tab, Ctrl+Shift+Tab and Ctrl+1 to Ctrl+4 move the keyboard focus between the side panels. Each panel keeps its last
    // focused control. The accent line of the panel follows the focus.
    public class PanelFocusCycleTests : IDisposable
    {
        private sealed record Screen(Views.MainWindow Window, MainViewModel ViewModel, SidePanelHost Left, SidePanelHost Right)
        {
            public ProjectTree Tree => ViewModel.ProjectTree;
            public ProjectTreeView View => Left.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            public TreeNode LayerNode => Tree.CompositionNodes()[0].Children[0].Children[0];
            public TreeViewItem LayerItem => ItemOf(View, LayerNode);
            public ToggleButton RightPicker => Right.FindControl<ToggleButton>("pickerToggle")!;
            public object? Focused => Window.FocusManager!.GetFocusedElement();
        }

        private Screen? screen;

        public void Dispose()
        {
            screen?.Window.Close();
            Dispatcher.UIThread.RunJobs();
        }

        // The tree on the left, the tabs on the right. One composition with one layer, and the layer is selected. With focusTree,
        // the focus is on the layer row.
        private Screen Show(bool focusTree = true)
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();
            TestServiceProviderFactory.ExpandAll(viewModel.ProjectTree);
            TestServiceProviderFactory.Pump();

            var hosts = window.GetVisualDescendants().OfType<SidePanelHost>()
                .OrderBy(host => host.TranslatePoint(default, window)!.Value.X).ToList();
            screen = new Screen(window, viewModel, hosts[0], hosts[1]);
            screen.Tree.SelectedNode = screen.LayerNode;
            Settle(window);
            if (focusTree)
            {
                screen.LayerItem.Focus();
                Settle(window);
            }

            return screen;
        }

        private static void Press(Screen screen, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            screen.Window.KeyPressQwerty(key, modifiers);
            screen.Window.KeyReleaseQwerty(key, modifiers);
            Settle(screen.Window);
        }

        private static bool IsIn(SidePanelHost panel, object? focused) =>
            focused is Visual visual && panel.IsVisualAncestorOf(visual);

        private static bool IsLineOn(SidePanelHost panel) => panel.FindControl<Border>("focusLine")!.IsEffectivelyVisible;

        [AvaloniaFact]
        public void Panels_OfTheMainWindow_AreTheTwoSidePanelsFromLeftToRight()
        {
            var screen = Show();

            Assert.Equal(new[] { screen.Left, screen.Right }, screen.ViewModel.PanelFocus.Panels());
        }

        [AvaloniaFact]
        public void Press_CtrlTab_MovesTheFocusToTheNextPanelAndBack()
        {
            var screen = Show();

            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control);

            Assert.True(IsIn(screen.Right, screen.Focused));
            Assert.True(IsLineOn(screen.Right));
            Assert.False(IsLineOn(screen.Left));

            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control);

            Assert.Same(screen.LayerItem, screen.Focused);
            Assert.True(IsLineOn(screen.Left));
            Assert.False(IsLineOn(screen.Right));
        }

        [AvaloniaFact]
        public void Press_CtrlShiftTab_MovesTheFocusToThePreviousPanel()
        {
            var screen = Show();

            // The left panel is the first panel, so the previous panel is the last panel.
            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.True(IsIn(screen.Right, screen.Focused));

            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Same(screen.LayerItem, screen.Focused);
        }

        [AvaloniaFact]
        public void Press_CtrlTabTwice_PutsTheFocusBackOnTheLastFocusedControl()
        {
            var screen = Show();
            screen.RightPicker.Focus();
            Settle(screen.Window);

            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control);
            Assert.Same(screen.LayerItem, screen.Focused);

            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control);
            Assert.Same(screen.RightPicker, screen.Focused);
        }

        [AvaloniaFact]
        public void Press_CtrlNumber_JumpsToThatPanel()
        {
            var screen = Show();

            Press(screen, PhysicalKey.Digit2, RawInputModifiers.Control);
            Assert.True(IsIn(screen.Right, screen.Focused));
            Assert.True(IsLineOn(screen.Right));

            Press(screen, PhysicalKey.Digit1, RawInputModifiers.Control);
            Assert.Same(screen.LayerItem, screen.Focused);
            Assert.True(IsLineOn(screen.Left));

            // There are two panels: Ctrl+3 and Ctrl+4 do nothing.
            Press(screen, PhysicalKey.Digit3, RawInputModifiers.Control);
            Assert.Same(screen.LayerItem, screen.Focused);
            Press(screen, PhysicalKey.Digit4, RawInputModifiers.Control);
            Assert.Same(screen.LayerItem, screen.Focused);
        }

        // With no last focused control, the focus goes to the cursor row of the tree.
        [AvaloniaFact]
        public void Press_CtrlOneIntoATreePanelNeverFocused_FocusesTheCursorRow()
        {
            var screen = Show(focusTree: false);
            screen.RightPicker.Focus();
            Settle(screen.Window);

            Press(screen, PhysicalKey.Digit1, RawInputModifiers.Control);

            Assert.Same(screen.LayerItem, screen.Focused);
        }

        // Ctrl+Tab in a text box moves the focus to the next panel. It types nothing.
        [AvaloniaFact]
        public void Press_CtrlTabInATextBox_MovesTheFocusAndKeepsTheText()
        {
            var screen = Show();
            var box = screen.Left.GetVisualDescendants().OfType<TextBox>().First();
            box.Focus();
            Settle(screen.Window);
            var text = box.Text;

            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control);

            Assert.True(IsIn(screen.Right, screen.Focused));
            Assert.Equal(text, box.Text);

            // The text box is the last focused control of the left panel.
            Press(screen, PhysicalKey.Tab, RawInputModifiers.Control);
            Assert.Same(box, screen.Focused);
        }

        [AvaloniaFact]
        public void FocusTreeCommand_FocusInOtherPanel_FocusesTheCursorRow()
        {
            var screen = Show(focusTree: false);
            screen.RightPicker.Focus();
            Settle(screen.Window);

            screen.ViewModel.PanelFocus.FocusTreeCommand.Execute(null);
            Settle(screen.Window);

            Assert.Same(screen.LayerItem, screen.Focused);
            Assert.True(IsLineOn(screen.Left));
        }

        // Tree.Focus has no default gesture. A gesture that the user sets works.
        [AvaloniaFact]
        public void Press_GestureSetForTreeFocus_FocusesTheCursorRow()
        {
            var screen = Show(focusTree: false);
            screen.ViewModel.Shortcuts.Set("Tree.Focus", new KeyGesture(Key.T, KeyModifiers.Control));
            screen.RightPicker.Focus();
            Settle(screen.Window);

            Press(screen, PhysicalKey.T, RawInputModifiers.Control);

            Assert.Same(screen.LayerItem, screen.Focused);
        }
    }
}
