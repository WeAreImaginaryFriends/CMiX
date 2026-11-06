using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;

namespace CMiX.Studio.Avalonia.Tests
{
    // An item that is moved to another list stays selected, in the tree and in the lists.
    public class ProjectTreeSelectionAfterDropTests
    {
        // A holds two layers, the first with two entities. B holds one layer.
        private static Scene CreateScene() => TreeFixtures.CreateScene(fill: scene =>
        {
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
        });

        [Fact]
        public async Task Drop_AnEntityOnAnotherLayer_SelectsItInTheTreeAndTheNewList()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[1];
            var node = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(0)).Children[1];
            scene.Tree.SelectedNode = node;

            await scene.Tree.DropAsync(node, Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(1)), DropPosition.Inside);

            var selected = scene.Tree.SelectedNode;
            Assert.NotNull(selected);
            Assert.Same(entity, selected!.Item);
            Assert.Same(Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(1)).Children[0], selected);
            Assert.Same(entity, scene.LayerOfA(1).ModelEntityManager.SelectedItem);
            Assert.Same(scene.LayerOfA(1), scene.A.LayerManager.SelectedItem);
        }

        [Fact]
        public async Task Drop_AnItem_OpensTheRowsThatHoldIt()
        {
            var scene = CreateScene();
            var node = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(0)).Children[0];
            var target = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(1));
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
        public async Task Drop_AnEntityThatIsNotSelected_SelectsIt()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[0];
            var node = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(0)).Children[0];

            await scene.Tree.DropAsync(node, Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(1)), DropPosition.Inside);

            Assert.Same(entity, scene.Tree.SelectedNode!.Item);
        }

        [Fact]
        public async Task Drop_ALayerOnAnotherComposition_SelectsIt()
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
        public async Task Drop_AnItem_MovesAndSelectsInOneUndoStep()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[1];
            var node = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(0)).Children[1];
            scene.Undo.Clear();

            await scene.Tree.DropAsync(node, Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(1)), DropPosition.Inside);
            scene.Undo.Undo();

            Assert.Contains(entity, scene.LayerOfA(0).ModelEntityManager.ManagerData.Items);
            Assert.Empty(scene.LayerOfA(1).ModelEntityManager.ManagerData.Items);
        }

        [Fact]
        public async Task Drop_InTheSameList_KeepsTheSelection()
        {
            var scene = CreateScene();
            var entity = scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[0];
            var group = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(0));
            scene.Tree.SelectedNode = group.Children[0];

            await scene.Tree.DropAsync(group.Children[0], group.Children[1], DropPosition.After);

            Assert.Same(entity, scene.Tree.SelectedNode!.Item);
        }

        [Fact]
        public async Task Drop_AnEntityThatTwoLayersShare_SelectsTheNodeInTheTargetLayer()
        {
            var scene = CreateScene();
            scene.A.LayerManager.AddItem(typeof(Layer));
            var entity = (Core.Prefabs.IPrefab)scene.LayerOfA(0).ModelEntityManager.ManagerData.Items[0];
            scene.LayerOfA(2).ModelEntityManager.AddExistingItem(entity);
            var node = Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(2)).Children[0];

            await scene.Tree.DropAsync(node, Scene.Group<EntitiesGroup>(scene.LayerNodeOfA(1)), DropPosition.Inside);

            var selected = scene.Tree.SelectedNode;
            Assert.Same(entity, selected!.Item);
            Assert.Same(scene.LayerNodeOfA(1), selected.Parent!.Parent);
        }

        [Fact]
        public async Task Drop_ABlockedMove_KeepsTheSelection()
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
