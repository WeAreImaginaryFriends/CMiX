using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CMiX.Core.Compositing;
using CMiX.Studio.Avalonia.Views;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tree of the project stands in its own column, left of the tabs, and is always visible.
    // The tree has the whole height of that column. The master beat is under the tabs only.
    public class MainWindowTreeTests
    {
        [AvaloniaFact]
        public void TheTree_StandsLeftOfTheTabs_AndStartsAtTheirTop()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();

            var tree = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var tabs = TestServiceProviderFactory.MainTabControl(window);

            Assert.Same(viewModel.ProjectTree, tree.DataContext);
            Assert.True(tree.IsVisible);
            Assert.True(tree.Bounds.Width > 0);

            var treeLeft = tree.TranslatePoint(new(0, 0), window)!.Value;
            var tabsLeft = tabs.TranslatePoint(new(0, 0), window)!.Value;
            Assert.True(treeLeft.X + tree.Bounds.Width <= tabsLeft.X, "The tree is not left of the tabs.");
            Assert.Equal(tabsLeft.Y, treeLeft.Y);
        }

        [AvaloniaFact]
        public void TheTreeAndTheTabs_HaveExactlyTheSameWidth()
        {
            var (_, window, _) = TestServiceProviderFactory.ShowMainWindow();
            var tree = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var columns = tree.GetVisualAncestors().OfType<Grid>().First(grid => grid.ColumnDefinitions.Count == 2).ColumnDefinitions;

            Assert.True(columns[0].ActualWidth > 0);
            Assert.Equal(columns[0].ActualWidth, columns[1].ActualWidth);
        }

        [AvaloniaFact]
        public void TheMasterBeat_IsUnderTheTabsOnly_AndTheTreeHasTheWholeHeightOfItsColumn()
        {
            var (_, window, _) = TestServiceProviderFactory.ShowMainWindow();
            var tree = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var tabs = TestServiceProviderFactory.MainTabControl(window);
            var beat = window.GetVisualDescendants().OfType<MasterBeatControl>().Single();

            var tabsOrigin = tabs.TranslatePoint(new(0, 0), window)!.Value;
            var beatOrigin = beat.TranslatePoint(new(0, 0), window)!.Value;
            var treeOrigin = tree.TranslatePoint(new(0, 0), window)!.Value;

            // The beat is in the right column: it starts and ends inside the width of the tabs.
            Assert.True(beatOrigin.X >= tabsOrigin.X, "The master beat starts left of the tabs.");
            Assert.True(beatOrigin.X + beat.Bounds.Width <= tabsOrigin.X + tabs.Bounds.Width, "The master beat ends right of the tabs.");

            // It is under the tabs, and the tabs end where it starts.
            Assert.True(beatOrigin.Y >= tabsOrigin.Y + tabs.Bounds.Height - 0.5, "The master beat is not under the tabs.");

            // The tree goes down to the bottom of the box of the beat (the box has a padding of 8 around the beat).
            var beatBox = beat.GetVisualAncestors().OfType<Border>().First();
            var beatBoxBottom = beatBox.TranslatePoint(new(0, beatBox.Bounds.Height), window)!.Value.Y;
            Assert.Equal(beatBoxBottom, treeOrigin.Y + tree.Bounds.Height, 0.5);
            Assert.True(tree.Bounds.Height > tabs.Bounds.Height, "The tree is not higher than the tabs.");
        }

        [AvaloniaFact]
        public void TheTreeRows_HaveTheSameSpaceOnAllFourSides()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            var tree = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            TestServiceProviderFactory.Pump();
            var row = tree.GetVisualDescendants().OfType<TreeViewItem>().First();
            var rowOrigin = row.TranslatePoint(new(0, 0), tree)!.Value;

            Assert.Equal(8, rowOrigin.X, 0.5);
            Assert.Equal(8, tree.Bounds.Width - (rowOrigin.X + row.Bounds.Width), 0.5);
            Assert.Equal(8, rowOrigin.Y, 0.5);

            // The right space of 8 is the room that the scroll bar has (ScrollBarGutter), so the list has no margin on the right.
            // The bottom is 8 as the left and the top, and the list has no padding of its own.
            var treeView = tree.GetVisualDescendants().OfType<TreeView>().First();
            Assert.Equal(new Thickness(8, 8, 0, 8), treeView.Margin);
            Assert.Equal(new Thickness(0), treeView.Padding);
        }

        [AvaloniaFact]
        public void TheTabs_KeepTheirWidthOfBefore()
        {
            var (_, window, _) = TestServiceProviderFactory.ShowMainWindow();

            var tabs = TestServiceProviderFactory.MainTabControl(window);

            Assert.True(tabs.Bounds.Width >= 400, "The tabs are narrower than before.");
        }

        [AvaloniaFact]
        public void AnAddedCompositionAndLayer_AppearInTheTreeOfTheWindow()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            var tree = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var before = tree.GetVisualDescendants().OfType<TreeViewItem>().Count();

            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.SelectedItem;
            composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();

            // A composition, a layer, and the three groups of the layer.
            Assert.Equal(before + 5, tree.GetVisualDescendants().OfType<TreeViewItem>().Count());
        }
    }
}
