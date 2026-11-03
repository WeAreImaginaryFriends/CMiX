using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Prefabs;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tree of the project on screen: one row per node, and a drag that follows the rules of TreeDropService.
    public class ProjectTreeViewTests
    {
        private sealed class Screen
        {
            public Window Window { get; init; } = null!;
            public ProjectTreeView View { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Project Project { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public TreeView TreeView => View.GetVisualDescendants().OfType<TreeView>().First();

            public TreeViewItem RowOf(TreeNode node) =>
                View.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

            // A point in the header of a row, as a fraction of its height.
            public Point PointIn(TreeNode node, double fraction)
            {
                var header = RowOf(node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "PART_LayoutRoot");
                return header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height * fraction), Window)!.Value;
            }

            public void Drag(TreeNode source, TreeNode target, double fraction, RawDragEventType last)
            {
                var data = new DataObject();
                data.Set(ProjectTreeView.NodeFormat, source);
                var point = PointIn(target, fraction);
                var effects = DragDropEffects.Move;
                Window.DragDrop(point, RawDragEventType.DragEnter, data, effects);
                Window.DragDrop(point, RawDragEventType.DragOver, data, effects);
                if (last == RawDragEventType.Drop)
                    Window.DragDrop(point, RawDragEventType.Drop, data, effects);
                Dispatcher.UIThread.RunJobs();
            }
        }

        // A holds two layers, B holds one.
        private static Screen Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            project.CompositionManager.AddItem(typeof(Composition));
            var a = (Composition)project.CompositionManager.ManagerData.Items[0];
            var b = (Composition)project.CompositionManager.ManagerData.Items[1];
            a.LayerManager.AddItem(typeof(Layer));
            a.LayerManager.AddItem(typeof(Layer));
            b.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            var window = new Window { Content = view, Width = 400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return new Screen { Window = window, View = view, Tree = tree, Project = project, A = a, B = b };
        }

        private static int CountNodes(TreeNode node) => 1 + node.Children.Sum(CountNodes);

        [AvaloniaFact]
        public void TheView_ShowsOneRowPerNode()
        {
            var screen = Show();

            var rows = screen.View.GetVisualDescendants().OfType<TreeViewItem>().Count();

            Assert.Equal(screen.Tree.Roots.Sum(CountNodes), rows);
        }

        [AvaloniaFact]
        public void ARowOfAGroup_ShowsTheNameOfTheGroup()
        {
            var screen = Show();
            var group = screen.Tree.Roots[0].Children[0].Children[0];

            var texts = screen.RowOf(group).GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text);

            Assert.Contains("Entities", texts);
        }

        [AvaloniaFact]
        public void ASelectedRow_SelectsTheNode()
        {
            var screen = Show();
            var node = screen.Tree.Roots[0].Children[1];

            screen.TreeView.SelectedItem = node;

            Assert.Same(node, screen.Tree.SelectedNode);
            Assert.Same(screen.A.LayerManager.ManagerData.Items[1], screen.A.LayerManager.SelectedItem);
        }

        [AvaloniaFact]
        public void ADragOverTheLowerPartOfARow_MarksTheRowAndAnEndedDragClearsIt()
        {
            var screen = Show();
            var row = screen.RowOf(screen.Tree.Roots[1]);

            screen.Drag(screen.Tree.Roots[0], screen.Tree.Roots[1], 0.9, RawDragEventType.DragOver);
            Assert.Contains(":dropafter", row.Classes);

            screen.Window.DragDrop(screen.PointIn(screen.Tree.Roots[1], 0.9), RawDragEventType.DragLeave,
                new DataObject(), DragDropEffects.Move);
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(":dropafter", row.Classes);
        }

        [AvaloniaFact]
        public void ADragOverTheMiddleOfACompositionRow_MarksItAsInside()
        {
            var screen = Show();
            var layerNode = screen.Tree.Roots[0].Children[0];

            screen.Drag(layerNode, screen.Tree.Roots[1], 0.5, RawDragEventType.DragOver);

            Assert.Contains(":dropinside", screen.RowOf(screen.Tree.Roots[1]).Classes);
        }

        [AvaloniaFact]
        public void ADragOverATargetThatIsNotAllowed_MarksNothing()
        {
            var screen = Show();
            var group = screen.Tree.Roots[0].Children[0].Children[0];

            screen.Drag(screen.Tree.Roots[0], group, 0.5, RawDragEventType.DragOver);

            Assert.DoesNotContain(":dropinside", screen.RowOf(group).Classes);
            Assert.DoesNotContain(":dropbefore", screen.RowOf(group).Classes);
            Assert.DoesNotContain(":dropafter", screen.RowOf(group).Classes);
        }

        [AvaloniaFact]
        public void ADropOnTheLowerPartOfACompositionRow_ReordersTheCompositions()
        {
            var screen = Show();
            var nodeA = screen.Tree.Roots[0];
            var nodeB = screen.Tree.Roots[1];

            screen.Drag(nodeA, nodeB, 0.9, RawDragEventType.Drop);

            Assert.Equal(new IControl[] { screen.B, screen.A }, screen.Project.CompositionManager.ManagerData.Items);
            Assert.DoesNotContain(":dropafter", screen.RowOf(nodeB).Classes);
        }

        [AvaloniaFact]
        public void ADropOfALayerOnTheMiddleOfACompositionRow_MovesTheLayerThere()
        {
            var screen = Show();
            var moved = screen.A.LayerManager.ManagerData.Items[0];

            screen.Drag(screen.Tree.Roots[0].Children[0], screen.Tree.Roots[1], 0.5, RawDragEventType.Drop);

            Assert.Single(screen.A.LayerManager.ManagerData.Items);
            Assert.Same(moved, screen.B.LayerManager.ManagerData.Items[1]);
        }

        [AvaloniaFact]
        public void ADropOfALayerWithASharedEntity_ChangesNothing()
        {
            var screen = Show();
            var layer = (Layer)screen.A.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)layer.ModelEntityManager.ManagerData.Items[0];
            ((Layer)screen.A.LayerManager.ManagerData.Items[1]).ModelEntityManager.AddExistingItem(entity);

            screen.Drag(screen.Tree.Roots[0].Children[0], screen.Tree.Roots[1], 0.5, RawDragEventType.Drop);

            Assert.Equal(2, screen.A.LayerManager.ManagerData.Items.Count);
            Assert.Single(screen.B.LayerManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADropOnATargetThatIsNotAllowed_ChangesNothing()
        {
            var screen = Show();
            var group = screen.Tree.Roots[0].Children[0].Children[0];

            screen.Drag(screen.Tree.Roots[1], group, 0.5, RawDragEventType.Drop);

            Assert.Equal(new IControl[] { screen.A, screen.B }, screen.Project.CompositionManager.ManagerData.Items);
        }
    }
}
