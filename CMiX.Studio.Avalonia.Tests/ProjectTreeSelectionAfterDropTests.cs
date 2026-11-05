using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // An item that is moved to another list stays selected, in the tree and in the lists.
    public class ProjectTreeSelectionAfterDropTests
    {
        private sealed class Scene
        {
            public Project Project { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public UndoManager Undo { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;
            public Layer LayerOfA(int index) => (Layer)A.LayerManager.ManagerData.Items[index];
            public TreeNode LayerNodeOfA(int index) => Tree.Roots[0].Children[index];
            public static TreeNode Group(TreeNode layerNode, string header) => layerNode.Children.Single(group => group.Header == header);
        }

        // A holds two layers, the first with two entities. B holds one layer.
        private static Scene CreateScene()
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
            var first = (Layer)a.LayerManager.ManagerData.Items[0];
            first.ModelEntityManager.AddItem(typeof(Entity));
            first.ModelEntityManager.AddItem(typeof(Entity));

            var undo = provider.GetRequiredService<UndoManager>();
            return new Scene { Project = project, Tree = new ProjectTree(project, undo), Undo = undo, A = a, B = b };
        }

        [Fact]
        public async Task AnEntityMovedToAnotherLayer_IsSelectedInTheTree_AndInTheNewList()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[1];
            var node = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[1];
            scene.Tree.SelectedNode = node;

            await scene.Tree.DropAsync(node, Scene.Group(scene.LayerNodeOfA(1), "Entities"), DropPosition.Inside);

            var selected = scene.Tree.SelectedNode;
            Assert.NotNull(selected);
            Assert.Same(entity, selected!.Item);
            Assert.Same(Scene.Group(scene.LayerNodeOfA(1), "Entities").Children[0], selected);
            Assert.Same(entity, scene.LayerOfA(1).ModelEntityManager.SelectedItem);
            Assert.Same(scene.LayerOfA(1), scene.A.LayerManager.SelectedItem);
        }

        [Fact]
        public async Task ADrop_OpensTheRowsThatHoldTheItem_ButNotTheItem()
        {
            var scene = CreateScene();
            var node = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[0];
            var target = Scene.Group(scene.LayerNodeOfA(1), "Entities");
            TestServiceProviderFactory.ExpandAll(scene.Tree);
            target.IsExpanded = false;
            scene.LayerNodeOfA(1).IsExpanded = false;
            scene.Tree.Roots[0].IsExpanded = false;

            await scene.Tree.DropAsync(node, target, DropPosition.Inside);

            Assert.True(target.IsExpanded);
            Assert.True(scene.LayerNodeOfA(1).IsExpanded);
            Assert.True(scene.Tree.Roots[0].IsExpanded);
            Assert.False(target.Children[0].IsExpanded);
        }

        [Fact]
        public async Task AnEntityThatWasNotSelected_IsSelectedAfterTheMove()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[0];
            var node = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[0];

            await scene.Tree.DropAsync(node, Scene.Group(scene.LayerNodeOfA(1), "Entities"), DropPosition.Inside);

            Assert.Same(entity, scene.Tree.SelectedNode!.Item);
        }

        [Fact]
        public async Task ALayerMovedToAnotherComposition_IsSelectedAfterTheMove()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(1);
            var node = scene.LayerNodeOfA(1);
            scene.Tree.SelectedNode = node;

            await scene.Tree.DropAsync(node, scene.Tree.Roots[1], DropPosition.Inside);

            Assert.Same(layer, scene.Tree.SelectedNode!.Item);
            Assert.Same(scene.Tree.Roots[1], scene.Tree.SelectedNode.Parent);
            Assert.Same(layer, scene.B.LayerManager.SelectedItem);
        }

        [Fact]
        public async Task TheMoveAndTheSelection_AreOneUndoStep()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[1];
            var node = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[1];
            scene.Undo.Clear();

            await scene.Tree.DropAsync(node, Scene.Group(scene.LayerNodeOfA(1), "Entities"), DropPosition.Inside);
            scene.Undo.Undo();

            Assert.Contains(entity, scene.LayerOfA(0).ModelEntityManager.ManagerData.Items);
            Assert.Empty(scene.LayerOfA(1).ModelEntityManager.ManagerData.Items);
        }

        [Fact]
        public async Task AMoveInTheSameList_KeepsTheSelection()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[0];
            var group = Scene.Group(scene.LayerNodeOfA(0), "Entities");
            scene.Tree.SelectedNode = group.Children[0];

            await scene.Tree.DropAsync(group.Children[0], group.Children[1], DropPosition.After);

            Assert.Same(entity, scene.Tree.SelectedNode!.Item);
        }

        [Fact]
        public async Task ABlockedMove_ChangesNoSelection()
        {
            var scene = CreateScene();
            var node = scene.LayerNodeOfA(0);
            var layer = scene.LayerOfA(0);
            scene.LayerOfA(1).ModelEntityManager.AddExistingItem((Core.Prefabs.IPrefab)layer.ModelEntityManager.ManagerData.Items[0]);
            scene.Tree.SelectedNode = scene.LayerNodeOfA(1);

            await scene.Tree.DropAsync(node, scene.Tree.Roots[1], DropPosition.Inside);

            Assert.Same(scene.LayerOfA(1), scene.Tree.SelectedNode!.Item);
        }
    }
}
