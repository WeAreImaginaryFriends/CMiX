using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Compositing;
using CMiX.Studio.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // The scroll bar of a list or of the tree lies over the content. The content has the same width with a bar and without one.
    public class ScrollBarOverlayTests
    {
        private static ScrollBar VerticalBarOf(Control control) =>
            control.GetVisualDescendants().OfType<ScrollBar>().First(bar => bar.Orientation == global::Avalonia.Layout.Orientation.Vertical);

        // The width of the first row of a list with the number of rows. Many rows need a bar, few rows do not.
        private static (double Width, bool BarShown) FirstRowOfAList(int count)
        {
            var list = new ListBox { Width = 200, Height = 96 };
            for (var i = 0; i < count; i++)
                list.Items.Add(new Border { Height = 24, Tag = "row" });
            var window = new Window { Content = list, Width = 300, Height = 200 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var row = list.GetVisualDescendants().OfType<ListBoxItem>().First();
            var width = row.Bounds.Width;
            var shown = VerticalBarOf(list).IsVisible;
            window.Close();
            return (width, shown);
        }

        [AvaloniaFact]
        public void AListWithManyRows_ShowsTheBar_AndTheRowsKeepTheirWidth()
        {
            var few = FirstRowOfAList(2);
            var many = FirstRowOfAList(20);

            Assert.False(few.BarShown);
            Assert.True(many.BarShown);
            Assert.Equal(few.Width, many.Width);
        }

        [AvaloniaFact]
        public void TheBarOfAList_LiesOverTheContent_AtTheRightEdge()
        {
            var list = new ListBox { Width = 200, Height = 96 };
            for (var i = 0; i < 20; i++)
                list.Items.Add(new Border { Height = 24 });
            var window = new Window { Content = list, Width = 300, Height = 200 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var bar = VerticalBarOf(list);
            var presenter = list.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
            var barRight = bar.TranslatePoint(new Point(bar.Bounds.Width, 0), window)!.Value.X;
            var presenterRight = presenter.TranslatePoint(new Point(presenter.Bounds.Width, 0), window)!.Value.X;

            Assert.True(bar.IsVisible);
            Assert.True(bar.Bounds.Width > 0);
            Assert.True(barRight <= presenterRight, "The bar is outside the content.");
            Assert.True(bar.Bounds.Right >= presenter.Bounds.Right - 0.5 - bar.Margin.Right, "The bar is not at the right edge.");
            window.Close();
        }

        // The tree with a composition and the number of layers: many layers need a bar.
        private static (double RowWidth, bool BarShown) FirstRowOfTheTree(int layers)
        {
            var (provider, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.SelectedItem;
            for (var i = 0; i < layers; i++)
                composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();
            window.UpdateLayout();

            var tree = window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var treeView = tree.GetVisualDescendants().OfType<TreeView>().First();
            var row = tree.GetVisualDescendants().OfType<TreeViewItem>().First();
            var result = (row.Bounds.Width, VerticalBarOf(treeView).IsVisible);
            window.Close();
            return result;
        }

        [AvaloniaFact]
        public void TheTree_ShowsTheBarWithManyRows_AndTheRowsKeepTheirWidth()
        {
            var few = FirstRowOfTheTree(1);
            var many = FirstRowOfTheTree(60);

            Assert.False(few.BarShown);
            Assert.True(many.BarShown);
            Assert.Equal(few.RowWidth, many.RowWidth);
        }
    }
}
