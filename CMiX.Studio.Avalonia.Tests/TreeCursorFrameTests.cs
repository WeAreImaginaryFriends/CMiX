using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.AttachedProperties;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeTestScreen;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // The cursor row of the tree has the class cursor and shows CursorFrame: a thick bright frame while the tree has the focus,
    // a thin grey frame when it does not.
    public class TreeCursorFrameTests : IDisposable
    {
        private Views.MainWindow? window;

        public void Dispose() => window?.Close();

        private sealed record Screen(Views.MainWindow Window, MainViewModel ViewModel, ProjectTreeView View)
        {
            public ProjectTree Tree => ViewModel.ProjectTree;
            public TreeNode LayerNode(int index) => Tree.CompositionNodes()[0].Children[0].Children[index];
        }

        // One composition with two layers. The first layer is selected, and its row has the focus.
        private Screen Show()
        {
            var (_, mainWindow, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            window = mainWindow;
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();
            TestServiceProviderFactory.ExpandAll(viewModel.ProjectTree);
            TestServiceProviderFactory.Pump();
            var view = mainWindow.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var screen = new Screen(mainWindow, viewModel, view);
            screen.Tree.SelectedNode = screen.LayerNode(0);
            Settle(mainWindow);
            ItemOf(view, screen.LayerNode(0)).Focus();
            Settle(mainWindow);
            return screen;
        }

        private static Color Brush(string key) => ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

        private static Thickness Thickness(string key) => (Thickness)Application.Current!.FindResource(key)!;

        private static TreeNode NextRow(Screen screen, TreeNode node)
        {
            var rows = screen.Tree.VisibleNodes().ToList();
            return rows[rows.IndexOf(node) + 1];
        }

        [AvaloniaFact]
        public void IsCursor_AfterDown_FollowsTheCursorNode()
        {
            var screen = Show();
            var first = screen.LayerNode(0);
            var next = NextRow(screen, first);

            screen.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            Settle(screen.Window);

            Assert.Same(next, screen.Tree.CursorNode);
            Assert.True(next.IsCursor);
            Assert.False(first.IsCursor);
            Assert.Contains(CursorRow.CursorClass, ItemOf(screen.View, next).Classes);
            Assert.DoesNotContain(CursorRow.CursorClass, ItemOf(screen.View, first).Classes);
        }

        [AvaloniaFact]
        public void CursorFrame_TreeFocused_IsTheThinBrightFrame()
        {
            var screen = Show();
            var node = screen.Tree.CursorNode!;

            var frame = Part(screen.View, node, "CursorFrame");

            Assert.True(frame.IsVisible);
            Assert.Equal(Brush("CursorFrameBrush"), ((ISolidColorBrush)frame.BorderBrush!).Color);
            Assert.Equal((Thickness)screen.View.FindResource("TreeSelectionThickness")!, frame.BorderThickness);
        }

        [AvaloniaFact]
        public void CursorFrame_FocusOutsideTheTree_IsTheThinGreyFrame()
        {
            var screen = Show();
            var node = screen.Tree.CursorNode!;
            var search = screen.Window.GetVisualDescendants().OfType<TreeSearchBox>().First().GetVisualDescendants().OfType<TextBox>().First();

            search.Focus();
            Settle(screen.Window);

            var frame = Part(screen.View, node, "CursorFrame");
            Assert.False(screen.View.GetVisualDescendants().OfType<TreeView>().First().IsKeyboardFocusWithin);
            Assert.True(frame.IsVisible);
            Assert.Equal(Brush("CursorFrameInactiveBrush"), ((ISolidColorBrush)frame.BorderBrush!).Color);
            Assert.Equal((Thickness)screen.View.FindResource("TreeSelectionThickness")!, frame.BorderThickness);
        }

        [AvaloniaFact]
        public void CursorFrame_RowThatIsNotTheCursor_IsHidden()
        {
            var screen = Show();
            var other = NextRow(screen, screen.Tree.CursorNode!);

            Assert.False(Part(screen.View, other, "CursorFrame").IsVisible);
        }
    }
}
