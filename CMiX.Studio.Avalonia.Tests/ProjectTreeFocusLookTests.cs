using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Tests.Support;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;

namespace CMiX.Studio.Avalonia.Tests
{
    // The row with the keyboard focus shows the focus frame of the row only: the tree and its rows have no dotted focus frame of Avalonia.
    public class ProjectTreeFocusLookTests : IDisposable
    {
        private TreeTestScreen? screen;

        public void Dispose() => screen?.Dispose();

        [AvaloniaFact]
        public void Focus_ARowWithTheKeyboard_ShowsNoDottedFrame()
        {
            var scene = CreateScene([2]);
            screen = TreeTestScreen.Show(scene.Tree, 400, 600);
            var item = TreeTestScreen.ItemOf(screen.View, scene.LayerNodeOfA(0));

            item.Focus(NavigationMethod.Tab);
            screen.Settle();

            Assert.True(item.IsFocused);
            Assert.Null(item.FocusAdorner);
            Assert.Null(screen.View.GetVisualDescendants().OfType<TreeView>().First().FocusAdorner);
            Assert.All(screen.View.GetVisualDescendants().OfType<TreeViewItem>(), row => Assert.Null(row.FocusAdorner));
        }
    }
}
