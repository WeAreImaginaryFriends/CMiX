using CMiX.Core.Compositing;
using CMiX.Core.Networking.Messages;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Prefabs.Messages;
using CMiX.Core.Undo;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // A layer moves from the layer list of one composition into the layer list of another.
    public class MoveItemToManagerTests
    {
        private sealed class Scene
        {
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;
            public UndoManager Undo { get; init; } = null!;
            public IServiceProvider Provider { get; init; } = null!;

            public Layer LayerOfA(int index) => (Layer)A.LayerManager.ManagerData.Items[index];
            public Layer LayerOfB(int index) => (Layer)B.LayerManager.ManagerData.Items[index];
        }

        // Composition A holds two layers, composition B holds one.
        private static Scene CreateScene()
        {
            var provider = TestServiceProviderFactory.Create();
            var compositions = provider.GetRequiredService<PrefabManager>();
            compositions.AddItem(typeof(Composition));
            compositions.AddItem(typeof(Composition));
            var a = (Composition)compositions.ManagerData.Items[0];
            var b = (Composition)compositions.ManagerData.Items[1];

            a.LayerManager.AddItem(typeof(Layer));
            a.LayerManager.AddItem(typeof(Layer));
            b.LayerManager.AddItem(typeof(Layer));

            var undo = provider.GetRequiredService<UndoManager>();
            undo.Clear();
            return new Scene { A = a, B = b, Undo = undo, Provider = provider };
        }

        [Fact]
        public void MoveItemToManager_ALayer_LeavesTheSourceAndEntersTheTargetAtTheIndex()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            var stays = scene.LayerOfA(1);
            var other = scene.LayerOfB(0);

            var result = scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.True(result.Moved);
            Assert.Empty(result.Blocking);
            Assert.Equal(new IControl[] { stays }, scene.A.LayerManager.ManagerData.Items);
            Assert.Equal(new IControl[] { moved, other }, scene.B.LayerManager.ManagerData.Items);
        }

        [Fact]
        public void MoveItemToManager_AnIndexPastTheEnd_AppendsTheLayer()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            var other = scene.LayerOfB(0);

            scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 99);

            Assert.Equal(new IControl[] { other, moved }, scene.B.LayerManager.ManagerData.Items);
        }

        [Fact]
        public void MoveItemToManager_ALayer_GivesTheLayerAndItsItemsTheCompositionIdOfTheTarget()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)moved.ModelEntityManager.ManagerData.Items[0];
            Assert.Equal(scene.A.ID, entity.CompositionID);

            scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.Equal(scene.B.ID, moved.CompositionID);
            Assert.Equal(scene.B.ID, entity.CompositionID);
        }

        [Fact]
        public void MoveItemToManager_ALayer_KeepsItsItems()
        {
            var scene = CreateScene();
            var repository = scene.Provider.GetRequiredService<ControlRepository>();
            var moved = scene.LayerOfA(0);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            var entity = moved.ModelEntityManager.ManagerData.Items[0];

            scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.Same(moved, repository.GetControl(moved.ID));
            Assert.Same(entity, repository.GetControl(entity.ID));
            Assert.Same(entity, Assert.Single(moved.ModelEntityManager.ManagerData.Items));
        }

        [Fact]
        public void MoveItemToManager_ALayer_IsOneUndoStep()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            var stays = scene.LayerOfA(1);
            var other = scene.LayerOfB(0);

            scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);
            scene.Undo.Undo();

            Assert.Equal(new IControl[] { moved, stays }, scene.A.LayerManager.ManagerData.Items);
            Assert.Equal(new IControl[] { other }, scene.B.LayerManager.ManagerData.Items);
            Assert.Equal(scene.A.ID, moved.CompositionID);
        }

        [Fact]
        public void MoveItemToManager_AnEntitySharedWithAnotherLayerOfTheSource_ChangesNothing()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            var stays = scene.LayerOfA(1);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)moved.ModelEntityManager.ManagerData.Items[0];
            stays.ModelEntityManager.AddExistingItem(entity);
            scene.Undo.Clear();

            var result = scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.False(result.Moved);
            Assert.Same(entity, Assert.Single(result.Blocking));
            Assert.Equal(2, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.Single(scene.B.LayerManager.ManagerData.Items);
            Assert.Equal(scene.A.ID, moved.CompositionID);
            Assert.False(scene.Undo.CanUndo);
        }

        [Fact]
        public void MoveItemToManager_AnEntitySharedWithALayerOfTheTarget_IsAllowed()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            moved.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)moved.ModelEntityManager.ManagerData.Items[0];
            scene.LayerOfB(0).ModelEntityManager.AddExistingItem(entity);

            var result = scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.True(result.Moved);
            Assert.Contains(moved, scene.B.LayerManager.ManagerData.Items);
        }

        [Fact]
        public void MoveItemToManager_TheSameList_DoesNothing()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);

            var result = scene.A.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 1);

            Assert.Same(MoveResult.None, result);
            Assert.Equal(2, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.False(scene.Undo.CanUndo);
        }

        [Fact]
        public void MoveItemToManager_AnItemThatIsNotInTheSource_DoesNothing()
        {
            var scene = CreateScene();
            var notInA = scene.LayerOfB(0);

            scene.B.LayerManager.MoveItemToManager(notInA, scene.A.LayerManager, 0);

            Assert.Single(scene.B.LayerManager.ManagerData.Items);
            Assert.Equal(2, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.False(scene.Undo.CanUndo);
        }

        [Fact]
        public void MoveItemToManager_ASourceWithoutTheItem_ReportsNothingHappened()
        {
            var scene = CreateScene();
            var notInA = scene.LayerOfB(0);

            var result = scene.B.LayerManager.MoveItemToManager(notInA, scene.A.LayerManager, 0);

            Assert.Same(MoveResult.None, result);
            Assert.False(result.Moved);
            Assert.Empty(result.Blocking);
        }

        [Fact]
        public void MoveItemToManager_ARefusedItem_IsNotMovedAndWritesNoUndoStep()
        {
            var scene = CreateScene();
            var moved = scene.LayerOfA(0);
            scene.B.LayerManager.CanAddExistingItem = control => !ReferenceEquals(control, moved);

            var result = scene.B.LayerManager.MoveItemToManager(moved, scene.A.LayerManager, 0);

            Assert.Same(MoveResult.None, result);
            Assert.Equal(2, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.DoesNotContain(moved, scene.B.LayerManager.ManagerData.Items);
            Assert.False(scene.Undo.CanUndo);
        }

        [Fact]
        public void Receive_AnAddThenARemoveOfTheSameLayer_MovesTheSameInstance()
        {
            var sender = TestServiceProviderFactory.Create();
            var senderA = sender.GetRequiredService<PrefabManager>();
            var senderB = sender.GetRequiredService<PrefabManager>();
            senderA.AddItem(typeof(Layer));
            var layer = (Layer)senderA.SelectedItem;
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var factory = sender.GetRequiredService<MessageFactory>();

            var receiver = TestServiceProviderFactory.Create();
            var repository = receiver.GetRequiredService<ControlRepository>();
            var a = receiver.GetRequiredService<PrefabManager>();
            var b = receiver.GetRequiredService<PrefabManager>();
            a.ManagerData.ID = senderA.ManagerData.ID;
            b.ManagerData.ID = senderB.ManagerData.ID;
            a.Receive(factory.CreateMessage<MessageAddItem>(senderA.ManagerData.ID, layer, 0));
            var received = a.ManagerData.Items[0];

            // The sender adds the layer to the target first, then removes it from the source.
            b.Receive(factory.CreateMessage<MessageAddItem>(senderB.ManagerData.ID, layer, 0));
            a.Receive(factory.CreateMessage<MessageRemoveItem>(senderA.ManagerData.ID, layer, -1));

            Assert.Empty(a.ManagerData.Items);
            Assert.Same(received, Assert.Single(b.ManagerData.Items));
            Assert.Same(received, repository.GetControl(layer.ID));
            Assert.Single(repository.Entities);
        }
    }
}
