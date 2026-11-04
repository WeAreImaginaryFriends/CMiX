using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Modulation.Modifiers;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tree shows what an item holds: its textures, its filters and its modifiers, as groups under it.
    public class ProjectTreeGroupsTests
    {
        private sealed class Scene
        {
            public Project Project { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Composition Composition { get; init; } = null!;
            public Layer Layer { get; init; } = null!;

            public TreeNode CompositionNode => Tree.Roots[0];
            public TreeNode LayerNode => CompositionNode.Children[0];
            public static TreeNode Group(TreeNode node, string header) => node.Children.Single(child => child.Item == null && child.Header == header);
            public static string[] Headers(TreeNode node) => node.Children.Where(child => child.Item == null).Select(child => child.Header).ToArray();
        }

        // A composition with two layers.
        private static Scene CreateScene()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            return new Scene { Project = project, Tree = tree, Composition = composition, Layer = (Layer)composition.LayerManager.ManagerData.Items[0] };
        }

        [Fact]
        public void ACompositionNode_HasTheLayersThenTheGroupsOfFiltersAndModifiers()
        {
            var scene = CreateScene();

            Assert.Equal(new[] { "Filters", "Modifiers" }, Scene.Headers(scene.CompositionNode));
            Assert.Equal(new[] { true, true, false, false }, scene.CompositionNode.Children.Select(child => child.Item != null));
            Assert.Same(scene.Composition.TextureModifierManager, Scene.Group(scene.CompositionNode, "Filters").ChildManager);
            Assert.Same(scene.Composition.ModifierManager, Scene.Group(scene.CompositionNode, "Modifiers").ChildManager);
        }

        [Fact]
        public void ALayerNode_HasItsThreeListsThenFiltersAndModifiers()
        {
            var scene = CreateScene();

            Assert.Equal(new[] { "Entities", "Cameras", "Lights", "Filters", "Modifiers" }, Scene.Headers(scene.LayerNode));
            Assert.Same(scene.Layer.TextureModifierManager, Scene.Group(scene.LayerNode, "Filters").ChildManager);
            Assert.Same(scene.Layer.ModifierManager, Scene.Group(scene.LayerNode, "Modifiers").ChildManager);
        }

        [Theory]
        [InlineData(typeof(Entity), new[] { "Textures", "Modifiers" })]
        [InlineData(typeof(FullscreenQuad), new[] { "Textures" })]
        [InlineData(typeof(TextEntity), new[] { "Modifiers" })]
        public void AnEntityNode_HasTheGroupsOfWhatItHolds(Type type, string[] expected)
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(type);

            var entity = Scene.Group(scene.LayerNode, "Entities").Children[0];

            Assert.Equal(expected, Scene.Headers(entity));
        }

        [Fact]
        public void ACameraNode_AndALightNode_HaveTheirModifiers()
        {
            var scene = CreateScene();
            scene.Layer.CameraManager.AddItem(typeof(Camera));
            scene.Layer.LightManager.AddItem(typeof(LightEntity));

            Assert.Equal(new[] { "Modifiers" }, Scene.Headers(Scene.Group(scene.LayerNode, "Cameras").Children[0]));
            Assert.Equal(new[] { "Modifiers" }, Scene.Headers(Scene.Group(scene.LayerNode, "Lights").Children[0]));
        }

        [Fact]
        public void AModifierAddedToAnEntity_AppearsUnderItsModifiersGroup_WithItsNameAndIcon()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];

            entity.ModifierManager.AddItem(typeof(TranslateModifier));

            var node = Assert.Single(Scene.Group(Scene.Group(scene.LayerNode, "Entities").Children[0], "Modifiers").Children);
            Assert.Same(entity.ModifierManager.ManagerData.Items[0], node.Item);
            Assert.Same(entity.ModifierManager, node.Owner);
            Assert.Equal(((IPrefab)node.Item!).PrefabService.Name.Value, node.Header);
            Assert.Equal("Modifier", node.IconKey);
        }

        [Fact]
        public void AFilterAddedToALayer_AppearsUnderItsFiltersGroup_WithTheFilterIcon()
        {
            var scene = CreateScene();

            scene.Layer.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));

            var node = Assert.Single(Scene.Group(scene.LayerNode, "Filters").Children);
            Assert.IsType<CMiX.Core.Texturing.Filters.Blur>(node.Item);
            Assert.Equal("Filter", node.IconKey);
        }

        [Fact]
        public void ATextureOfAnEntity_AppearsUnderItsTexturesGroup_AndHasItsOwnFilters()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];

            entity.Texture.DiffuseTexture.TextureManager.AddItem(typeof(CMiX.Core.Texturing.Sources.Image));

            var entityNode = Scene.Group(scene.LayerNode, "Entities").Children[0];
            var texture = Assert.Single(Scene.Group(entityNode, "Textures").Children);
            Assert.IsType<CMiX.Core.Texturing.Sources.Image>(texture.Item);
            Assert.Equal("Texture", texture.IconKey);
            Assert.Equal(new[] { "Filters", "Modulators" }, Scene.Headers(texture));
        }

        [Fact]
        public void TheGroupsOfAComposition_StayAfterTheLayers_WhenLayersAreAddedRemovedAndMoved()
        {
            var scene = CreateScene();

            scene.Composition.LayerManager.AddItem(typeof(Layer));
            Assert.Equal(new[] { true, true, true, false, false }, scene.CompositionNode.Children.Select(child => child.Item != null));

            scene.Composition.LayerManager.MoveItem(0, 2);
            Assert.Equal(new[] { true, true, true, false, false }, scene.CompositionNode.Children.Select(child => child.Item != null));
            Assert.Equal(scene.Composition.LayerManager.ManagerData.Items, scene.CompositionNode.Children.Where(child => child.Item != null).Select(child => child.Item));

            scene.Composition.LayerManager.RemoveItem(scene.Composition.LayerManager.ManagerData.Items[0]);
            Assert.Equal(new[] { true, true, false, false }, scene.CompositionNode.Children.Select(child => child.Item != null));
            Assert.Equal(new[] { "Filters", "Modifiers" }, Scene.Headers(scene.CompositionNode));
        }

        [Fact]
        public void TheGroupsOfAComposition_StayWhenTheListIsReset()
        {
            var scene = CreateScene();

            scene.Composition.LayerManager.ClearAll();

            Assert.Equal(new[] { "Filters", "Modifiers" }, Scene.Headers(scene.CompositionNode));
            Assert.DoesNotContain(scene.CompositionNode.Children, child => child.Item != null);
        }

        [Fact]
        public void AGroupNode_KnowsItsOwnerAndItsList_ForTheInspector()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];
            var entityNode = Scene.Group(scene.LayerNode, "Entities").Children[0];

            var modifiers = Assert.IsType<ModifiersGroup>(Scene.Group(entityNode, "Modifiers").Group);
            Assert.Same(entity, modifiers.Owner);
            Assert.Same(entity.ModifierManager, modifiers.Manager);
            Assert.Equal(typeof(Entity), modifiers.OwnerType);

            var textures = Assert.IsType<TexturesGroup>(Scene.Group(entityNode, "Textures").Group);
            Assert.Same(entity.Texture.DiffuseTexture.TextureManager, textures.Manager);

            var filters = Assert.IsType<FiltersGroup>(Scene.Group(scene.LayerNode, "Filters").Group);
            Assert.Equal(typeof(Layer), filters.OwnerType);
            Assert.IsType<EntitiesGroup>(Scene.Group(scene.LayerNode, "Entities").Group);
        }

        [Fact]
        public void SelectingAModifierNode_SelectsTheChainDownToTheModifier()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];
            entity.ModifierManager.AddItem(typeof(TranslateModifier));
            entity.ModifierManager.AddItem(typeof(ScaleModifier));
            var node = Scene.Group(Scene.Group(scene.LayerNode, "Entities").Children[0], "Modifiers").Children[0];

            scene.Tree.SelectedNode = node;

            Assert.Same(scene.Composition, scene.Project.CompositionManager.SelectedItem);
            Assert.Same(scene.Layer, scene.Composition.LayerManager.SelectedItem);
            Assert.Same(entity, scene.Layer.ModelEntityManager.SelectedItem);
            Assert.Same(node.Item, entity.ModifierManager.SelectedItem);
        }

        [Fact]
        public void TheGroupOfModifiers_OffersTheModifiersOfItsOwner_AndAddsTheChosenOne()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];
            var group = Scene.Group(Scene.Group(scene.LayerNode, "Entities").Children[0], "Modifiers");

            var adds = scene.Tree.GetAdds(group);

            Assert.Equal(AddableTypes.ForOwner(typeof(Entity)).Select(AddableTypes.LabelOf), adds.Select(add => add.Header));
            Assert.Contains(adds, add => add.Header == AddableTypes.LabelOf(typeof(TranslateModifier)));
            adds.Single(add => add.Header == AddableTypes.LabelOf(typeof(TranslateModifier))).Execute!();
            Assert.IsType<TranslateModifier>(Assert.Single(entity.ModifierManager.ManagerData.Items));
            Assert.Single(group.Children);
        }

        [Fact]
        public void TheGroupOfFilters_OffersTheFilters_AndAddsTheChosenOne()
        {
            var scene = CreateScene();
            var group = Scene.Group(scene.LayerNode, "Filters");

            var adds = scene.Tree.GetAdds(group);

            Assert.Equal(AddableTypes.ForInterface(typeof(ITextureFilter)).Select(AddableTypes.LabelOf), adds.Select(add => add.Header));
            adds.Single(add => add.Header == AddableTypes.LabelOf(typeof(Blur))).Execute!();
            Assert.IsType<Blur>(Assert.Single(scene.Layer.TextureModifierManager.ManagerData.Items));
        }

        [Fact]
        public void TheGroupOfTextures_OffersTheTextureSources_AndAddsTheChosenOne()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];
            var group = Scene.Group(Scene.Group(scene.LayerNode, "Entities").Children[0], "Textures");

            var adds = scene.Tree.GetAdds(group);

            Assert.Contains(adds, add => add.Header == AddableTypes.LabelOf(typeof(CMiX.Core.Texturing.Sources.Image)));
            adds.Single(add => add.Header == AddableTypes.LabelOf(typeof(CMiX.Core.Texturing.Sources.Image))).Execute!();
            Assert.IsType<CMiX.Core.Texturing.Sources.Image>(Assert.Single(entity.Texture.DiffuseTexture.TextureManager.ManagerData.Items));
        }

        [Fact]
        public void AModifierAFilterAndATexture_HaveAModulatorsGroup_WithTheirModulators()
        {
            var scene = CreateScene();
            scene.Layer.TextureModifierManager.AddItem(typeof(Blur));
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];
            entity.ModifierManager.AddItem(typeof(TranslateModifier));
            entity.Texture.DiffuseTexture.TextureManager.AddItem(typeof(CMiX.Core.Texturing.Sources.Image));
            var entityNode = Scene.Group(scene.LayerNode, "Entities").Children[0];
            var nodes = new[]
            {
                Scene.Group(scene.LayerNode, "Filters").Children[0],
                Scene.Group(entityNode, "Modifiers").Children[0],
                Scene.Group(entityNode, "Textures").Children[0]
            };

            foreach (var node in nodes)
            {
                var owner = (CMiX.Core.Modulation.IModulatable)node.Item!;
                owner.ModulatorManager.AddItem(typeof(LFOModulator));

                var group = Scene.Group(node, "Modulators");
                Assert.IsType<ModulatorsGroup>(group.Group);
                Assert.Same(owner.ModulatorManager, group.ChildManager);
                var modulator = Assert.Single(group.Children);
                Assert.IsType<LFOModulator>(modulator.Item);
                Assert.Equal("Modulator", modulator.IconKey);
                Assert.Equal("Modulator", group.IconKey);
            }
        }

        [Fact]
        public void TheGroupOfModulators_OffersTheModulators_AndAddsTheChosenOne()
        {
            var scene = CreateScene();
            scene.Layer.TextureModifierManager.AddItem(typeof(Blur));
            var filter = (Blur)scene.Layer.TextureModifierManager.ManagerData.Items[0];
            var filterNode = Scene.Group(scene.LayerNode, "Filters").Children[0];

            var adds = scene.Tree.GetAdds(Scene.Group(filterNode, "Modulators"));

            Assert.Equal(AddableTypes.ForInterface(typeof(IModulator)).Select(AddableTypes.LabelOf), adds.Select(add => add.Header));
            adds.Single(add => add.Header == AddableTypes.LabelOf(typeof(LFOModulator))).Execute!();
            Assert.IsType<LFOModulator>(Assert.Single(filter.ModulatorManager.ManagerData.Items));
            Assert.Contains(scene.Tree.GetAdds(filterNode), add => add.Header == "Add Modulator" && add.Secondary);
        }

        [Fact]
        public void AnItem_HasSubmenusForWhatItCanHold_AndTheyAreNotTheDefaultRow()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var entityNode = Scene.Group(scene.LayerNode, "Entities").Children[0];

            var adds = scene.Tree.GetAdds(entityNode);
            Assert.Equal(new[] { "Add Texture", "Add Modifier" }, adds.Select(add => add.Header));
            Assert.All(adds, add => Assert.True(add.Secondary));
            Assert.All(adds, add => Assert.NotEmpty(add.Children!));

            Assert.Equal(new[] { "Add Layer", "Add Filter", "Add Modifier" }, scene.Tree.GetAdds(scene.CompositionNode).Select(add => add.Header));
            Assert.False(scene.Tree.GetAdds(scene.CompositionNode)[0].Secondary);

            var modifier = adds.Single(add => add.Header == "Add Modifier").Children!.Single(add => add.Header == AddableTypes.LabelOf(typeof(ScaleModifier)));
            modifier.Execute!();
            Assert.IsType<ScaleModifier>(Assert.Single(((Entity)entityNode.Item!).ModifierManager.ManagerData.Items));
        }

        [Fact]
        public void TheModifiersOfAnEntity_CanBeReorderedInTheirOwnList_AndNotMovedElsewhere()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            var first = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[0];
            var second = (Entity)scene.Layer.ModelEntityManager.ManagerData.Items[1];
            first.ModifierManager.AddItem(typeof(TranslateModifier));
            first.ModifierManager.AddItem(typeof(ScaleModifier));
            second.ModifierManager.AddItem(typeof(TranslateModifier));
            var entities = Scene.Group(scene.LayerNode, "Entities");
            var modifiers = Scene.Group(entities.Children[0], "Modifiers");
            var foreign = Scene.Group(entities.Children[1], "Modifiers").Children[0];
            var service = new TreeDropService();

            Assert.True(service.CanDrop(modifiers.Children[1], modifiers.Children[0], DropPosition.Before));
            Assert.False(service.CanDrop(modifiers.Children[0], foreign, DropPosition.Before));
            Assert.False(service.CanDrop(modifiers.Children[0], Scene.Group(entities.Children[1], "Modifiers"), DropPosition.Inside));

            var moved = modifiers.Children[1].Item;
            service.Drop(modifiers.Children[1], modifiers.Children[0], DropPosition.Before);

            Assert.Same(moved, first.ModifierManager.ManagerData.Items[0]);
        }
    }
}
