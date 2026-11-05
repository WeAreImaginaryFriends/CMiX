using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The icon of a row has the color of the name. The kind of the row shows in the fill behind it.
    public class ProjectTreeRowIconTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        private (ProjectTreeView View, ProjectTree Tree) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 400, Height = 400 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            Dispatcher.UIThread.RunJobs();
            return (view, tree);
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static Color ColorOf(IBrush? brush) => ((ISolidColorBrush)brush!).Color;

        [AvaloniaFact]
        public void TheIconOfEveryRow_IsFullyOpaque_AndHasTheColorOfTheNames()
        {
            var (view, tree) = Show();
            var foreground = ColorOf((IBrush)Application.Current!.FindResource("ForegroundBrush")!);
            var group = tree.Roots[0].Children[0].Children[0];
            var name = ItemOf(view, group).GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == group.Header);

            foreach (var node in new[] { tree.Roots[0], tree.Roots[0].Children[0], group })
            {
                var icon = ItemOf(view, node).GetVisualDescendants().OfType<AppIcon>().First();
                Assert.Equal(1.0, icon.Opacity);
                Assert.Equal(foreground, ColorOf(icon.Foreground));
            }

            Assert.Equal(foreground, ColorOf(name.Foreground));
        }
    }
}
