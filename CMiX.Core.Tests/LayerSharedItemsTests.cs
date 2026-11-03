using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // A layer can move to another composition only when no other layer of its composition holds the same items.
    public class LayerSharedItemsTests
    {
        private static (Layer First, Layer Second, Layer Third) CreateLayers()
        {
            var provider = TestServiceProviderFactory.Create();
            var compositionManager = provider.GetRequiredService<PrefabManager>();
            compositionManager.AddItem(typeof(Composition));
            var composition = (Composition)compositionManager.SelectedItem;

            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            var layers = composition.LayerManager.ManagerData.Items.Cast<Layer>().ToList();
            return (layers[0], layers[1], layers[2]);
        }

        [Fact]
        public void GetSharedItems_NothingIsShared_IsEmpty()
        {
            var (first, second, _) = CreateLayers();
            first.ModelEntityManager.AddItem(typeof(Entity));
            second.ModelEntityManager.AddItem(typeof(Entity));

            Assert.Empty(first.GetSharedItems(new[] { first, second }));
        }

        [Fact]
        public void GetSharedItems_AnEntityHeldByAnotherLayer_IsListed()
        {
            var (first, second, _) = CreateLayers();
            first.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)first.ModelEntityManager.SelectedItem;
            second.ModelEntityManager.AddExistingItem(entity);

            var shared = first.GetSharedItems(new[] { first, second });

            Assert.Same(entity, Assert.Single(shared));
        }

        [Fact]
        public void GetSharedItems_ACameraAndALightHeldByAnotherLayer_AreListed()
        {
            var (first, second, _) = CreateLayers();
            first.CameraManager.AddItem(typeof(Camera));
            first.LightManager.AddItem(typeof(LightEntity));
            var camera = (IPrefab)first.CameraManager.SelectedItem;
            var light = (IPrefab)first.LightManager.SelectedItem;
            second.CameraManager.AddExistingItem(camera);
            second.LightManager.AddExistingItem(light);

            var shared = first.GetSharedItems(new[] { second });

            Assert.Equal(new[] { camera, light }, shared);
        }

        [Fact]
        public void GetSharedItems_OnlyLayersOfTheListCount()
        {
            var (first, second, third) = CreateLayers();
            first.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)first.ModelEntityManager.SelectedItem;
            second.ModelEntityManager.AddExistingItem(entity);

            Assert.Empty(first.GetSharedItems(new[] { third }));
        }

        [Fact]
        public void GetSharedItems_TheLayerItselfInTheList_IsIgnored()
        {
            var (first, _, _) = CreateLayers();
            first.ModelEntityManager.AddItem(typeof(Entity));

            Assert.Empty(first.GetSharedItems(new[] { first }));
        }
    }
}
