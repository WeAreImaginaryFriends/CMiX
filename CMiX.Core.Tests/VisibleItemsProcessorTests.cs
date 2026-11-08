using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Processors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class VisibleItemsProcessorTests
    {
        private static (VisibleItemsProcessor Processor, Layer Layer) Create(int entityCount)
        {
            var provider = TestServiceProviderFactory.Create();
            var processor = provider.GetRequiredService<VisibleItemsProcessor>();
            var repository = provider.GetRequiredService<ControlRepository>();
            var collectionManager = provider.GetRequiredService<CollectionManager>();
            var layer = (Layer)provider.GetRequiredService<ControlFactory>().Create(typeof(Layer));
            repository.AddControl(layer, collectionManager);
            for (var i = 0; i < entityCount; i++)
                layer.ModelEntityManager.AddItem(typeof(Entity));

            return (processor, layer);
        }

        private static List<IControl> Items(PrefabManager manager) => manager.ManagerData.Items.ToList();

        [Fact]
        public void GetActiveIds_AList_ListsTheItemsInTheOrderOfTheList()
        {
            var (processor, layer) = Create(3);

            Assert.Equal(Items(layer.ModelEntityManager).Select(item => item.ID), processor.GetVisibleIds(layer.ModelEntityManager));
        }

        [Fact]
        public void GetActiveIds_AHiddenItem_LeavesTheListAndComesBack()
        {
            var (processor, layer) = Create(3);
            var items = Items(layer.ModelEntityManager);
            var hidden = (IPrefab)items[1];

            hidden.PrefabService.Visibility.Value = true;
            Assert.Equal(new[] { items[0].ID, items[2].ID }, processor.GetVisibleIds(layer.ModelEntityManager));

            hidden.PrefabService.Visibility.Value = false;
            Assert.Equal(items.Select(item => item.ID), processor.GetVisibleIds(layer.ModelEntityManager));
        }

        [Fact]
        public void GetActiveIds_AnItemAddedOrRemoved_FollowsTheList()
        {
            var (processor, layer) = Create(2);
            var first = Items(layer.ModelEntityManager)[0];
            Assert.Equal(2, processor.GetVisibleIds(layer.ModelEntityManager).Count);

            layer.ModelEntityManager.AddItem(typeof(Entity));
            Assert.Equal(3, processor.GetVisibleIds(layer.ModelEntityManager).Count);

            layer.ModelEntityManager.RemoveItem(first);
            Assert.DoesNotContain(first.ID, processor.GetVisibleIds(layer.ModelEntityManager));
        }

        [Fact]
        public void GetActiveIds_TwoLists_DoNotTouchEachOther()
        {
            var (processor, layer) = Create(2);

            Assert.Equal(2, processor.GetVisibleIds(layer.ModelEntityManager).Count);
            Assert.Empty(processor.GetVisibleIds(layer.CameraManager));
        }

        [Fact]
        public void GetActiveIds_ANullManager_IsEmpty()
        {
            var (processor, _) = Create(0);

            Assert.Empty(processor.GetVisibleIds(null));
        }

        [Fact]
        public void GetVisibleIds_AHiddenList_GivesNoItemAndKeepsTheStateOfTheItems()
        {
            var (processor, layer) = Create(3);
            var items = Items(layer.ModelEntityManager);
            ((IPrefab)items[1]).PrefabService.Visibility.Value = true;

            layer.ModelEntityManager.Hidden.Value = true;
            Assert.Empty(processor.GetVisibleIds(layer.ModelEntityManager));
            Assert.True(((IPrefab)items[1]).PrefabService.Visibility.Value);
            Assert.False(((IPrefab)items[0]).PrefabService.Visibility.Value);

            // The item that was hidden before stays hidden when the list shows again.
            layer.ModelEntityManager.Hidden.Value = false;
            Assert.Equal(new[] { items[0].ID, items[2].ID }, processor.GetVisibleIds(layer.ModelEntityManager));
        }

        [Fact]
        public void GetVisibleIds_AHiddenList_DoesNotHideTheOtherLists()
        {
            var (processor, layer) = Create(2);
            layer.CameraManager.AddItem(typeof(CMiX.Core.Rendering.Cameras.Camera));

            layer.ModelEntityManager.Hidden.Value = true;

            Assert.Single(processor.GetVisibleIds(layer.CameraManager));
        }

        [Fact]
        public void ToModel_AHiddenList_KeepsTheStateThroughFromModel()
        {
            var (_, layer) = Create(1);
            layer.ModelEntityManager.Hidden.Value = true;

            var model = (PrefabManagerModel)layer.ModelEntityManager.ToModel();

            Assert.True(model.Hidden.Value);
            layer.ModelEntityManager.Hidden.Value = false;
            layer.ModelEntityManager.FromModel(model);
            Assert.True(layer.ModelEntityManager.Hidden.Value);
        }

        [Fact]
        public void FromModel_AModelWithoutTheState_LeavesTheListVisible()
        {
            var (_, layer) = Create(1);

            layer.ModelEntityManager.FromModel(new PrefabManagerModel { ID = layer.ModelEntityManager.ID });

            Assert.False(layer.ModelEntityManager.Hidden.Value);
        }

        [Fact]
        public void Hidden_TheIdOfTheList_IsDerivedFromItAndFollowsIt()
        {
            var (_, layer) = Create(1);
            var manager = layer.ModelEntityManager;

            Assert.Equal(DerivedId.Of(manager.ManagerData.ID, "Hidden"), manager.Hidden.ID);

            manager.ManagerData.ID = Guid.NewGuid();
            Assert.Equal(DerivedId.Of(manager.ManagerData.ID, "Hidden"), manager.Hidden.ID);
            Assert.Equal(manager.Hidden.ID, ((PrefabManagerModel)manager.ToModel()).Hidden.ID);
        }
    }
}
