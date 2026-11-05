using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tree of compositions, layers and the items of a layer follows the lists and selects through them.
    public class ProjectTreeTests
    {
        private sealed class Scene
        {
            public Project Project { get; init; } = null!;
            public UndoManager Undo { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public Layer LayerOfA(int index) => (Layer)A.LayerManager.ManagerData.Items[index];
        }

        // Composition A holds two layers, composition B holds one. B is the selected composition.
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

            var undo = provider.GetRequiredService<UndoManager>();
            undo.Clear();
            return new Scene { Project = project, Undo = undo, Tree = new ProjectTree(project, undo), A = a, B = b };
        }

        private static string NameOf(IControl item) => ((IPrefab)item).PrefabService.Name.Value;

        // The node of a layer of A.
        private static TreeNode LayerNode(Scene scene, int index) => scene.Tree.Roots[0].Children[index];

        // The layers of a composition. The groups of the composition (filters and modifiers) come after them.
        private static IEnumerable<TreeNode> LayersOf(TreeNode composition) => composition.Children.Where(child => child.Item != null);

        private static TreeNode GroupNode(TreeNode layerNode, string header) =>
            layerNode.Children.Single(group => group.Header == header);

        [Fact]
        public void ANewItem_StartsCollapsed_AndDoesNotOpenTheRowThatHoldsIt()
        {
            var scene = CreateScene();
            var composition = scene.Tree.Roots[0];
            var before = composition.Children.Count(child => child.Item != null);
            composition.IsExpanded = false;

            scene.A.LayerManager.AddItem(typeof(Layer));

            Assert.False(composition.IsExpanded);
            var added = composition.Children[before];
            Assert.NotNull(added.Item);
            Assert.False(added.IsExpanded);
        }

        [Fact]
        public void Roots_AreTheCompositionsInTheOrderOfTheList()
        {
            var scene = CreateScene();

            Assert.Equal(new IControl[] { scene.A, scene.B }, scene.Tree.Roots.Select(node => node.Item));
            Assert.Equal(new[] { NameOf(scene.A), NameOf(scene.B) }, scene.Tree.Roots.Select(node => node.Header));
        }

        [Fact]
        public void ACompositionNode_HasTheLayersOfTheCompositionAsChildren()
        {
            var scene = CreateScene();

            Assert.Equal(scene.A.LayerManager.ManagerData.Items, LayersOf(scene.Tree.Roots[0]).Select(node => node.Item));
            Assert.Same(scene.A.LayerManager, scene.Tree.Roots[0].ChildManager);
            Assert.Same(scene.A.LayerManager, LayerNode(scene, 0).Owner);
        }

        [Fact]
        public void ALayerNode_HasTheGroupsEntitiesCamerasAndLights()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);

            var node = LayerNode(scene, 0);

            Assert.Equal(new[] { "Entities", "Cameras", "Lights", "Filters", "Modifiers" }, node.Children.Select(group => group.Header));
            Assert.All(node.Children, group => Assert.Null(group.Item));
            Assert.Same(layer.ModelEntityManager, node.Children[0].ChildManager);
            Assert.Same(layer.CameraManager, node.Children[1].ChildManager);
            Assert.Same(layer.LightManager, node.Children[2].ChildManager);
            Assert.Same(layer.TextureModifierManager, node.Children[3].ChildManager);
            Assert.Same(layer.ModifierManager, node.Children[4].ChildManager);
        }

        [Fact]
        public void AGroupNode_HasTheItemsOfItsListAsChildren()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.CameraManager.AddItem(typeof(Camera));
            layer.LightManager.AddItem(typeof(LightEntity));

            var node = LayerNode(scene, 0);

            Assert.Same(layer.ModelEntityManager.ManagerData.Items[0], Assert.Single(GroupNode(node, "Entities").Children).Item);
            Assert.Same(layer.CameraManager.ManagerData.Items[0], Assert.Single(GroupNode(node, "Cameras").Children).Item);
            Assert.Same(layer.LightManager.ManagerData.Items[0], Assert.Single(GroupNode(node, "Lights").Children).Item);
        }

        [Fact]
        public void AnAddedItem_AddsANode()
        {
            var scene = CreateScene();

            scene.A.LayerManager.AddItem(typeof(Layer));
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));

            Assert.Equal(3, LayersOf(scene.Tree.Roots[0]).Count());
            Assert.Single(GroupNode(LayerNode(scene, 0), "Entities").Children);
        }

        [Fact]
        public void ARemovedItem_RemovesItsNode()
        {
            var scene = CreateScene();

            scene.A.LayerManager.RemoveItem(scene.LayerOfA(0));

            Assert.Single(LayersOf(scene.Tree.Roots[0]));
        }

        [Fact]
        public void AMovedItem_MovesItsNode()
        {
            var scene = CreateScene();
            var first = scene.LayerOfA(0);

            scene.A.LayerManager.MoveItem(0, 1);

            Assert.Same(first, scene.Tree.Roots[0].Children[1].Item);
            Assert.Equal(scene.A.LayerManager.ManagerData.Items, LayersOf(scene.Tree.Roots[0]).Select(node => node.Item));
        }

        [Fact]
        public void AMoveFromAPeer_MovesItsNode()
        {
            var scene = CreateScene();
            var first = scene.LayerOfA(0);

            scene.A.LayerManager.ManagerData.Move(0, 1);

            Assert.Same(first, scene.Tree.Roots[0].Children[1].Item);
        }

        [Fact]
        public void ARenamedItem_ChangesTheHeader()
        {
            var scene = CreateScene();

            scene.LayerOfA(0).PrefabService.Name.Value = "Back";

            Assert.Equal("Back", LayerNode(scene, 0).Header);
        }

        [Fact]
        public void ALayerMovedToAnotherComposition_MovesItsNode()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);

            scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.Single(LayersOf(scene.Tree.Roots[0]));
            Assert.Equal(2, LayersOf(scene.Tree.Roots[1]).Count());
            Assert.Same(moved, scene.Tree.Roots[1].Children[0].Item);
            Assert.Same(scene.B.LayerManager, scene.Tree.Roots[1].Children[0].Owner);
        }

        [Fact]
        public void ADisposedTree_NoLongerFollowsTheLists()
        {
            var scene = CreateScene();
            var node = scene.Tree.Roots[0];

            scene.Tree.Dispose();
            scene.A.LayerManager.AddItem(typeof(Layer));

            Assert.Equal(2, LayersOf(node).Count());
        }

        [Fact]
        public void SelectingALayerNode_SelectsTheCompositionAndTheLayer()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);

            scene.Tree.SelectedNode = LayerNode(scene, 0);

            Assert.Same(scene.A, scene.Project.CompositionManager.SelectedItem);
            Assert.Same(layer, scene.A.LayerManager.SelectedItem);
        }

        [Fact]
        public void SelectingAnEntityNode_SelectsTheWholeChain()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var first = layer.ModelEntityManager.ManagerData.Items[0];

            scene.Tree.SelectedNode = GroupNode(LayerNode(scene, 0), "Entities").Children[0];

            Assert.Same(scene.A, scene.Project.CompositionManager.SelectedItem);
            Assert.Same(layer, scene.A.LayerManager.SelectedItem);
            Assert.Same(first, layer.ModelEntityManager.SelectedItem);
        }

        [Fact]
        public void SelectingAGroupNode_SelectsOnlyItsAncestors()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.ClearSelection();

            scene.Tree.SelectedNode = GroupNode(LayerNode(scene, 0), "Entities");

            Assert.Same(layer, scene.A.LayerManager.SelectedItem);
            Assert.Null(layer.ModelEntityManager.SelectedItem);
        }

        [Fact]
        public void SelectingANode_IsOneUndoStep()
        {
            var scene = CreateScene();
            var secondLayer = scene.LayerOfA(1);
            scene.Undo.Clear();

            scene.Tree.SelectedNode = LayerNode(scene, 0);
            Assert.Same(scene.A, scene.Project.CompositionManager.SelectedItem);
            Assert.Same(scene.LayerOfA(0), scene.A.LayerManager.SelectedItem);

            scene.Undo.Undo();

            Assert.Same(scene.B, scene.Project.CompositionManager.SelectedItem);
            Assert.Same(secondLayer, scene.A.LayerManager.SelectedItem);
        }

        [Fact]
        public void ASelectionInTheLists_MovesTheSelectedNode()
        {
            var scene = CreateScene();

            scene.A.LayerManager.SelectAt(0);

            Assert.Same(scene.LayerOfA(0), scene.Tree.SelectedNode!.Item);
        }

        [Fact]
        public void AnUnselectedNode_DoesNotChangeTheSelectionOfTheLists()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = LayerNode(scene, 0);
            scene.Undo.Clear();

            scene.Tree.SelectedNode = LayerNode(scene, 0);

            Assert.False(scene.Undo.CanUndo);
        }

        [Fact]
        public void ARemovedSelectedNode_MovesTheSelectionToTheParent()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = layer.ModelEntityManager.ManagerData.Items[0];
            scene.Tree.SelectedNode = GroupNode(LayerNode(scene, 0), "Entities").Children[0];

            layer.ModelEntityManager.RemoveItem(entity);

            Assert.Equal("Entities", scene.Tree.SelectedNode!.Header);
        }

        [Fact]
        public void ARemovedSelectedComposition_SelectsTheNeighborLikeTheList()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = scene.Tree.Roots[1];

            scene.Project.CompositionManager.RemoveItem(scene.B);

            Assert.Same(scene.A, scene.Project.CompositionManager.SelectedItem);
            Assert.Same(scene.A, scene.Tree.SelectedNode!.Item);
        }

        [Fact]
        public void TheOnlyCompositionRemoved_ClearsTheSelection()
        {
            var scene = CreateScene();
            scene.Project.CompositionManager.RemoveItem(scene.A);
            scene.Tree.SelectedNode = scene.Tree.Roots[0];

            scene.Project.CompositionManager.RemoveItem(scene.B);

            Assert.Null(scene.Tree.SelectedNode);
        }
    }
}
