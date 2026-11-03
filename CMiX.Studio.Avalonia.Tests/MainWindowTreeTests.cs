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
    public class MainWindowTreeTests
    {
        [AvaloniaFact]
        public void TheTree_StandsLeftOfTheTabsAndInsideTheirHeight()
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
            Assert.True(treeLeft.Y >= tabsLeft.Y, "The tree starts above the tabs.");
            Assert.True(treeLeft.Y + tree.Bounds.Height <= tabsLeft.Y + tabs.Bounds.Height, "The tree ends below the tabs.");
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
