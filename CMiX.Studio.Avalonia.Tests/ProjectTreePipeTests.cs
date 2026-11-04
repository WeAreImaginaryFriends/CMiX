using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The pipe: a line under the chevron of an open row that shows which rows are its children.
    public class ProjectTreePipeTests : IDisposable
    {
        private readonly List<Window> windows = new();

        public void Dispose()
        {
            foreach (var window in windows)
                window.Close();
        }

        private (Window Window, ProjectTreeView View, ProjectTree Tree) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            var window = new Window { Content = view, Width = 400, Height = 600 };
            windows.Add(window);
            window.Show();
            Settle(window);
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            return (window, view, tree);
        }

        private static void Settle(Window window)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        // Found in the template of the row itself: the rows below it have pipes too.
        private static Border PipeOf(TreeViewItem item) =>
            item.GetTemplateChildren().OfType<Border>().First(border => border.Name == "Pipe");

        private static ToggleButton ChevronOf(TreeViewItem item) =>
            item.GetTemplateChildren().OfType<ToggleButton>().First(button => button.Name == "PART_ExpandCollapseChevron");

        private static double CenterX(Window window, Control control) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, 0), window)!.Value.X;

        [AvaloniaFact]
        public void ThePipe_IsUnderTheChevron_AtEveryLevel()
        {
            var (window, view, tree) = Show();
            var composition = tree.Roots[0];
            var layer = composition.Children[0];

            foreach (var node in new[] { composition, layer })
            {
                var item = ItemOf(view, node);
                Assert.InRange(CenterX(window, PipeOf(item)), CenterX(window, ChevronOf(item)) - 1, CenterX(window, ChevronOf(item)) + 1);
            }
        }

        [AvaloniaFact]
        public void ThePipe_GoesDownAlongTheChildren_AndStartsBelowTheRow()
        {
            var (window, view, tree) = Show();
            var composition = tree.Roots[0];
            var item = ItemOf(view, composition);
            var pipe = PipeOf(item);
            var lastChild = ItemOf(view, composition.Children[^1]);

            var pipeTop = pipe.TranslatePoint(new Point(0, 0), window)!.Value.Y;
            var pipeBottom = pipeTop + pipe.Bounds.Height;
            var rowBottom = item.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder")
                .TranslatePoint(new Point(0, 24), window)!.Value.Y;
            var lastChildTop = lastChild.TranslatePoint(new Point(0, 0), window)!.Value.Y;

            Assert.True(pipe.IsEffectivelyVisible);
            Assert.True(pipeTop >= rowBottom + 3.5, "The pipe is less than 4 below the row.");
            var itemBottom = item.TranslatePoint(new Point(0, item.Bounds.Height), window)!.Value.Y;
            Assert.True(pipeBottom <= itemBottom - 3.5, "The pipe is less than 4 above the end of the last child.");
            Assert.True(pipeBottom > lastChildTop, "The pipe ends before the last child.");
        }

        [AvaloniaFact]
        public void ThePipe_IsHiddenForAClosedRow_AndForARowWithoutChildren()
        {
            var (window, view, tree) = Show();
            var composition = tree.Roots[0];
            var group = composition.Children[0].Children[0];
            Assert.Empty(group.Children);
            Assert.False(PipeOf(ItemOf(view, group)).IsEffectivelyVisible);

            composition.IsExpanded = false;
            Settle(window);

            Assert.False(PipeOf(ItemOf(view, composition)).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void ThePipe_TakesNoPointerInput_AndAddsNoHeightToTheRows()
        {
            var (window, view, tree) = Show();
            var composition = tree.Roots[0];
            var item = ItemOf(view, composition);

            Assert.False(((Control)PipeOf(item).Parent!).IsHitTestVisible);

            var heightBefore = item.Bounds.Height;
            composition.IsExpanded = false;
            composition.IsExpanded = true;
            Settle(window);

            Assert.Equal(heightBefore, item.Bounds.Height);
        }
    }
}
