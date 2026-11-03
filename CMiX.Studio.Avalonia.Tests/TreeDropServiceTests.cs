using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Rendering.Cameras;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The rules for a drag in the project tree: the same type in the same list, a layer to another composition.
    public class TreeDropServiceTests
    {
        private sealed class Scene
        {
            public Project Project { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public TreeNode NodeA => Tree.Roots[0];
            public TreeNode NodeB => Tree.Roots[1];
            public Layer LayerOfA(int index) => (Layer)A.LayerManager.ManagerData.Items[index];
            public TreeNode LayerNodeOfA(int index) => NodeA.Children[index];
            public TreeNode LayerNodeOfB(int index) => NodeB.Children[index];
            public static TreeNode Group(TreeNode layerNode, string header) => layerNode.Children.Single(group => group.Header == header);
        }

        // A holds two layers, B holds one.
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

            var undo = provider.GetRequiredService<CMiX.Core.Undo.UndoManager>();
            return new Scene { Project = project, Tree = new ProjectTree(project, undo), A = a, B = b };
        }

        private readonly TreeDropService service = new();

        [Fact]
        public void Drop_ACompositionAfterAnother_ReordersTheCompositions()
        {
            var scene = CreateScene();

            var outcome = service.Drop(scene.NodeA, scene.NodeB, DropPosition.After);

            Assert.True(outcome.Moved);
            Assert.Equal(new IControl[] { scene.B, scene.A }, scene.Project.CompositionManager.ManagerData.Items);
        }

        [Fact]
        public void Drop_ACompositionBeforeAnother_ReordersTheCompositions()
        {
            var scene = CreateScene();

            service.Drop(scene.NodeB, scene.NodeA, DropPosition.Before);

            Assert.Equal(new IControl[] { scene.B, scene.A }, scene.Project.CompositionManager.ManagerData.Items);
        }

        [Fact]
        public void Drop_ALayerAfterTheNextLayer_ReordersTheLayers()
        {
            var scene = CreateScene();
            var first = scene.LayerOfA(0);
            var second = scene.LayerOfA(1);

            service.Drop(scene.LayerNodeOfA(0), scene.LayerNodeOfA(1), DropPosition.After);

            Assert.Equal(new IControl[] { second, first }, scene.A.LayerManager.ManagerData.Items);
        }

        [Fact]
        public void Drop_AnItemWhereItAlreadyIs_DoesNothing()
        {
            var scene = CreateScene();

            Assert.False(service.Drop(scene.LayerNodeOfA(0), scene.LayerNodeOfA(1), DropPosition.Before).Moved);
            Assert.False(service.Drop(scene.LayerNodeOfA(1), scene.LayerNodeOfA(0), DropPosition.After).Moved);
        }

        [Fact]
        public void Drop_AnEntityBeforeAnotherEntity_ReordersTheGroup()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var first = layer.ModelEntityManager.ManagerData.Items[0];
            var second = layer.ModelEntityManager.ManagerData.Items[1];
            var group = Scene.Group(scene.LayerNodeOfA(0), "Entities");

            service.Drop(group.Children[1], group.Children[0], DropPosition.Before);

            Assert.Equal(new IControl[] { second, first }, layer.ModelEntityManager.ManagerData.Items);
        }

        [Fact]
        public void Drop_ALayerBeforeALayerOfAnotherComposition_MovesTheLayerThere()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);

            var outcome = service.Drop(scene.LayerNodeOfA(0), scene.LayerNodeOfB(0), DropPosition.Before);

            Assert.True(outcome.Moved);
            Assert.Single(scene.A.LayerManager.ManagerData.Items);
            Assert.Same(moved, scene.B.LayerManager.ManagerData.Items[0]);
        }

        [Fact]
        public void Drop_ALayerInsideAComposition_MovesTheLayerToTheEnd()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);

            service.Drop(scene.LayerNodeOfA(0), scene.NodeB, DropPosition.Inside);

            Assert.Equal(2, scene.B.LayerManager.ManagerData.Items.Count);
            Assert.Same(moved, scene.B.LayerManager.ManagerData.Items[1]);
        }

        [Fact]
        public void Drop_ALayerInsideItsOwnComposition_MovesTheLayerToTheEnd()
        {
            var scene = CreateScene();
            var first = scene.LayerOfA(0);

            service.Drop(scene.LayerNodeOfA(0), scene.NodeA, DropPosition.Inside);

            Assert.Same(first, scene.A.LayerManager.ManagerData.Items[1]);
        }

        [Fact]
        public void Drop_ALayerWithASharedEntity_StaysAndReturnsTheBlockingItems()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)moved.ModelEntityManager.ManagerData.Items[0];
            scene.LayerOfA(1).ModelEntityManager.AddExistingItem(entity);

            var outcome = service.Drop(scene.LayerNodeOfA(0), scene.NodeB, DropPosition.Inside);

            Assert.False(outcome.Moved);
            Assert.Same(entity, Assert.Single(outcome.Blocking));
            Assert.Equal(2, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.Single(scene.B.LayerManager.ManagerData.Items);
        }

        [Fact]
        public void CanDrop_ALayerWithASharedEntity_IsTrueSoTheUserCanReadTheReason()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            scene.LayerOfA(1).ModelEntityManager.AddExistingItem((IPrefab)moved.ModelEntityManager.ManagerData.Items[0]);

            Assert.True(service.CanDrop(scene.LayerNodeOfA(0), scene.NodeB, DropPosition.Inside));
        }

        [Fact]
        public void DescribeBlock_NamesTheLayerTheCompositionsAndTheItems()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)moved.ModelEntityManager.ManagerData.Items[0];
            scene.LayerOfA(1).ModelEntityManager.AddExistingItem(entity);

            var text = service.DescribeBlock(scene.LayerNodeOfA(0), scene.NodeB, new[] { entity });

            Assert.Contains($"\"{scene.LayerNodeOfA(0).Header}\"", text);
            Assert.Contains($"\"{scene.NodeA.Header}\"", text);
            Assert.Contains($"\"{scene.NodeB.Header}\"", text);
            Assert.Contains(entity.PrefabService.Name.Value, text);
            Assert.DoesNotContain((char)0x2013, text);
            Assert.DoesNotContain((char)0x2014, text);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void PositionFor_ALayerOverAComposition_IsInsideWhereverThePointerIs(double fraction)
        {
            var scene = CreateScene();

            Assert.Equal(DropPosition.Inside, service.PositionFor(scene.LayerNodeOfA(0), scene.NodeB, fraction));
        }

        [Theory]
        [InlineData(0.0, DropPosition.Before)]
        [InlineData(0.49, DropPosition.Before)]
        [InlineData(0.5, DropPosition.After)]
        [InlineData(1.0, DropPosition.After)]
        public void PositionFor_AnythingElse_IsBeforeInTheTopHalfAndAfterInTheBottomHalf(double fraction, DropPosition expected)
        {
            var scene = CreateScene();

            Assert.Equal(expected, service.PositionFor(scene.NodeA, scene.NodeB, fraction));
            Assert.Equal(expected, service.PositionFor(scene.LayerNodeOfA(0), scene.LayerNodeOfA(1), fraction));
        }

        [Fact]
        public void CanDrop_ACompositionInsideAComposition_IsFalse()
        {
            var scene = CreateScene();

            Assert.False(service.CanDrop(scene.NodeA, scene.NodeB, DropPosition.Inside));
        }

        [Fact]
        public void CanDrop_ALayerInsideALayerOrAGroup_IsFalse()
        {
            var scene = CreateScene();

            Assert.False(service.CanDrop(scene.LayerNodeOfA(0), scene.LayerNodeOfA(1), DropPosition.Inside));
            Assert.False(service.CanDrop(scene.LayerNodeOfA(0), Scene.Group(scene.LayerNodeOfA(1), "Entities"), DropPosition.Inside));
        }

        [Fact]
        public void CanDrop_ACompositionBetweenLayers_IsFalse()
        {
            var scene = CreateScene();

            Assert.False(service.CanDrop(scene.NodeA, scene.LayerNodeOfB(0), DropPosition.Before));
        }

        [Fact]
        public void CanDrop_AnEntityBeforeAnEntityOfAnotherLayer_IsFalse()
        {
            var scene = CreateScene();
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            scene.LayerOfA(1).ModelEntityManager.AddItem(typeof(Entity));
            var source = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[0];
            var target = Scene.Group(scene.LayerNodeOfA(1), "Entities").Children[0];

            Assert.False(service.CanDrop(source, target, DropPosition.Before));
        }

        [Fact]
        public void CanDrop_ACameraBeforeAnEntity_IsFalse()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);
            layer.CameraManager.AddItem(typeof(Camera));
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var camera = Scene.Group(scene.LayerNodeOfA(0), "Cameras").Children[0];
            var entity = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[0];

            Assert.False(service.CanDrop(camera, entity, DropPosition.Before));
        }

        [Fact]
        public void CanDrop_AGroup_CannotBeDragged()
        {
            var scene = CreateScene();
            var group = Scene.Group(scene.LayerNodeOfA(0), "Entities");

            Assert.False(service.CanDrop(group, scene.LayerNodeOfA(1), DropPosition.Before));
        }

        [Fact]
        public void CanDrop_BeforeOrAfterAGroup_IsFalse()
        {
            var scene = CreateScene();
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            var entity = Scene.Group(scene.LayerNodeOfA(0), "Entities").Children[0];
            var group = Scene.Group(scene.LayerNodeOfA(0), "Cameras");

            Assert.False(service.CanDrop(entity, group, DropPosition.Before));
            Assert.False(service.CanDrop(entity, group, DropPosition.After));
        }

        [Fact]
        public void CanDrop_ANodeOntoItselfOrItsOwnChild_IsFalse()
        {
            var scene = CreateScene();

            Assert.False(service.CanDrop(scene.NodeA, scene.NodeA, DropPosition.Before));
            Assert.False(service.CanDrop(scene.NodeA, scene.LayerNodeOfA(0), DropPosition.Before));
        }
    }
}
