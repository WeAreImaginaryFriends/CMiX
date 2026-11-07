using CMiX.Core.Compositing;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // ProjectTree.GetAddGroups gives the Add panel what it shows: the groups of what a node can take, from the interfaces of the node.
    public class ProjectTreeAddGroupsTests
    {
        // A composition with two layers.
        private static Scene CreateScene() => TreeFixtures.CreateScene([2]);

        private static string[] Headers(IEnumerable<TreeAddGroup> groups) => groups.Select(group => group.Header).ToArray();

        private static string[] ItemHeaders(TreeAddGroup group) => group.Items.Select(item => item.Header).ToArray();

        [Fact]
        public void GetAddGroups_ACompositionNode_OffersLayerFilterAndModifier()
        {
            var scene = CreateScene();

            var groups = scene.Tree.GetAddGroups(scene.CompositionNode);

            Assert.Equal(new[] { "Layer", "Filter", "Modifier" }, Headers(groups));
            Assert.Equal(new[] { "Layer" }, ItemHeaders(groups[0]));
        }

        [Fact]
        public void GetAddGroups_ALayerNode_OffersContentFilterAndModifier()
        {
            var scene = CreateScene();

            var groups = scene.Tree.GetAddGroups(scene.LayerNode);

            Assert.Equal(new[] { "Content", "Filter", "Modifier" }, Headers(groups));
            Assert.Equal(new[] { "Entity", "Fullscreen Quad", "Text", "Camera", "Light" }, ItemHeaders(groups[0]));
        }

        [Theory]
        [InlineData(typeof(Entity), new[] { "Texture", "Modifier" })]
        [InlineData(typeof(FullscreenQuad), new[] { "Texture" })]
        [InlineData(typeof(TextEntity), new[] { "Modifier" })]
        public void GetAddGroups_AnEntityNode_OffersWhatTheEntityHolds(Type type, string[] expected)
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(type);
            var entity = Scene.Group<EntitiesGroup>(scene.LayerNode).Children[0];

            Assert.Equal(expected, Headers(scene.Tree.GetAddGroups(entity)));
        }

        [Fact]
        public void GetAddGroups_ACameraOrALightNode_OffersOnlyModifiers()
        {
            var scene = CreateScene();
            scene.Layer.CameraManager.AddItem(typeof(Camera));
            scene.Layer.LightManager.AddItem(typeof(LightEntity));

            Assert.Equal(new[] { "Modifier" }, Headers(scene.Tree.GetAddGroups(Scene.Group<CamerasGroup>(scene.LayerNode).Children[0])));
            Assert.Equal(new[] { "Modifier" }, Headers(scene.Tree.GetAddGroups(Scene.Group<LightsGroup>(scene.LayerNode).Children[0])));
        }

        [Fact]
        public void GetAddGroups_NoNode_OffersTheProjectGroupOnly()
        {
            var scene = CreateScene();

            var groups = scene.Tree.GetAddGroups(null);

            Assert.Equal(new[] { "Project" }, Headers(groups));
            Assert.Equal(new[] { "Composition" }, ItemHeaders(groups[0]));
        }

        [Fact]
        public void GetAddGroups_WithTheProject_PutsTheProjectGroupLast()
        {
            var scene = CreateScene();

            var groups = scene.Tree.GetAddGroups(scene.LayerNode, withProject: true);

            Assert.Equal(new[] { "Content", "Filter", "Modifier", "Project" }, Headers(groups));
        }

        [Fact]
        public void GetAddGroups_AGroupRow_OffersOneGroupOfItsOwnList()
        {
            var scene = CreateScene();

            var cameras = scene.Tree.GetAddGroups(Scene.Group<CamerasGroup>(scene.LayerNode));
            var filters = scene.Tree.GetAddGroups(Scene.Group<FiltersGroup>(scene.LayerNode));

            var group = Assert.Single(cameras);
            Assert.Equal("Camera", group.Header);
            Assert.Equal(new[] { "Camera" }, ItemHeaders(group));
            Assert.Equal("Filter", Assert.Single(filters).Header);
        }

        [Fact]
        public void GetAddGroups_EveryNode_GivesItemsWithoutTheWordAdd()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            scene.Layer.CameraManager.AddItem(typeof(Camera));

            var headers = scene.All().SelectMany(node => scene.Tree.GetAddGroups(node, withProject: true)).SelectMany(ItemHeaders).ToList();

            Assert.NotEmpty(headers);
            Assert.DoesNotContain(headers, header => header.StartsWith("Add ", StringComparison.Ordinal));
        }

        [Fact]
        public void GetAddGroups_EveryNode_OffersTheSameAddsAsGetAdds()
        {
            var scene = CreateScene();
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            scene.Layer.ModelEntityManager.AddItem(typeof(FullscreenQuad));
            scene.Layer.CameraManager.AddItem(typeof(Camera));
            scene.Layer.LightManager.AddItem(typeof(LightEntity));

            foreach (var node in scene.All())
            {
                var expected = scene.Tree.GetAdds(node)
                    .SelectMany(add => add.Children ?? new[] { add })
                    .Select(add => add.Id)
                    .ToList();
                var actual = scene.Tree.GetAddGroups(node).SelectMany(group => group.Items).Select(item => item.Id).ToList();

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void GetAddGroups_TheItems_CarryTheKindOfWhatTheyAdd()
        {
            var scene = CreateScene();

            var groups = scene.Tree.GetAddGroups(scene.LayerNode);

            Assert.Equal(
                new TreeCategory?[] { TreeCategory.Entity, TreeCategory.Entity, TreeCategory.Entity, TreeCategory.Camera, TreeCategory.Light },
                groups[0].Items.Select(item => item.Category));
            Assert.Null(groups[0].Category);
            Assert.Equal(TreeCategory.Filter, groups[1].Category);
            Assert.All(groups[1].Items, item => Assert.Equal(TreeCategory.Filter, item.Category));
            Assert.All(groups[2].Items, item => Assert.Equal(TreeCategory.Modifier, item.Category));
        }

        [Fact]
        public void Execute_TheItemsOfALayerNode_AddWhatTheyName()
        {
            var scene = CreateScene();
            var content = scene.Tree.GetAddGroups(scene.LayerNode)[0];

            content.Items.Single(item => item.Header == "Camera").Execute!();
            content.Items.Single(item => item.Header == "Entity").Execute!();

            Assert.Single(scene.Layer.CameraManager.ManagerData.Items);
            Assert.Single(scene.Layer.ModelEntityManager.ManagerData.Items);
        }

        [Fact]
        public void Execute_TheLayerAndTheProjectItems_AddALayerAndAComposition()
        {
            var scene = CreateScene();
            var layers = scene.A.LayerManager.ManagerData.Items.Count;
            var compositions = scene.Project.CompositionManager.ManagerData.Items.Count;

            scene.Tree.GetAddGroups(scene.CompositionNode).Single(group => group.Header == "Layer").Items[0].Execute!();
            scene.Tree.GetAddGroups(scene.CompositionNode, withProject: true).Single(group => group.Header == "Project").Items[0].Execute!();

            Assert.Equal(layers + 1, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.Equal(compositions + 1, scene.Project.CompositionManager.ManagerData.Items.Count);
        }
    }
}
