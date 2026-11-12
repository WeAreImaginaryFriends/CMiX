using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;

namespace CMiX.Studio.Avalonia.Tests
{
    // Two views can show the same tree (both side panels). Each view gives its own hooks. A view that goes takes back only its own.
    public class TreeViewHooksTests
    {
        [Fact]
        public void DetachView_OneOfTwoViews_TheOtherViewStillAnswers()
        {
            var tree = CreateScene().Tree;
            var toggled = 0;
            var cancelled = 0;
            var first = new TreeViewHooks
            {
                IsPointerOverTree = () => true,
                IsTreeFocused = () => true,
                IsTextBoxFocused = () => true,
                ToggleDragRow = () => toggled++,
                CancelDrag = () => cancelled++
            };
            var second = new TreeViewHooks();
            tree.AttachView(first);
            tree.AttachView(second);

            tree.DetachView(second);

            Assert.True(tree.IsPointerOverTree());
            Assert.True(tree.IsTreeFocused());
            Assert.True(tree.IsTextBoxFocused());
            tree.ToggleDragRow();
            tree.CancelDrag();
            Assert.Equal(1, toggled);
            Assert.Equal(1, cancelled);
        }

        [Fact]
        public void AttachView_SecondView_DoesNotHideTheFirstView()
        {
            var tree = CreateScene().Tree;
            tree.AttachView(new TreeViewHooks { IsPointerOverTree = () => true });

            tree.AttachView(new TreeViewHooks());

            Assert.True(tree.IsPointerOverTree());
        }

        // The focus in the panel of one view is not in another panel, so every view must say that the focus is elsewhere.
        [Fact]
        public void IsFocusInOtherPanel_TwoViews_IsTrueOnlyWhenEveryViewSaysSo()
        {
            var tree = CreateScene().Tree;
            var focusInSecond = true;
            Assert.False(tree.IsFocusInOtherPanel());
            tree.AttachView(new TreeViewHooks { IsFocusInOtherPanel = () => true });
            tree.AttachView(new TreeViewHooks { IsFocusInOtherPanel = () => !focusInSecond });

            Assert.False(tree.IsFocusInOtherPanel());

            focusInSecond = false;
            Assert.True(tree.IsFocusInOtherPanel());
        }

        [Fact]
        public void DetachView_LastView_TheHooksAnswerNo()
        {
            var tree = CreateScene().Tree;
            var view = new TreeViewHooks { IsPointerOverTree = () => true, IsTreeFocused = () => true };
            tree.AttachView(view);

            tree.DetachView(view);

            Assert.False(tree.IsPointerOverTree());
            Assert.False(tree.IsTreeFocused());
        }

        [Fact]
        public void CloseRowsInView_TwoViews_GoesToTheFocusedViewOnce()
        {
            var tree = CreateScene().Tree;
            var asked = new List<string>();
            var closes = 0;
            tree.AttachView(new TreeViewHooks { CloseRowsInView = (_, close) => { asked.Add("first"); close(); } });
            tree.AttachView(new TreeViewHooks { IsTreeFocused = () => true, CloseRowsInView = (_, close) => { asked.Add("second"); close(); } });

            tree.CloseRowsInView(null, () => { closes++; return Array.Empty<TreeNode>(); });

            Assert.Equal(new[] { "second" }, asked);
            Assert.Equal(1, closes);
        }

        [AvaloniaFact]
        public void Unwatch_SecondViewLeaves_TheFocusedViewStillTellsTheTreeHasTheFocus()
        {
            var scene = CreateScene();
            var first = new ProjectTreeView { DataContext = scene.Tree };
            var second = new ProjectTreeView { DataContext = scene.Tree };
            var panel = new StackPanel { Children = { first, second } };
            var window = new Window { Content = panel, Width = 400, Height = 1100 };
            window.Show();
            TreeTestScreen.Settle(window);
            try
            {
                Assert.True(TreeTestScreen.ItemOf(first, scene.Tree.Roots[0]).Focus(NavigationMethod.Tab));
                Dispatcher.UIThread.RunJobs();
                Assert.True(scene.Tree.IsTreeFocused());

                panel.Children.Remove(second);
                Dispatcher.UIThread.RunJobs();

                Assert.True(scene.Tree.IsTreeFocused());
            }
            finally
            {
                window.Close();
            }
        }
    }
}
