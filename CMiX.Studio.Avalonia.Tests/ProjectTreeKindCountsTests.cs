using CMiX.Core.Compositing;
using CMiX.Core.Modulation.Modifiers;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // A row knows what it holds, by kind, so a collapsed row can tell the user.
    public class ProjectTreeKindCountsTests
    {
        private sealed class Scene
        {
            public ProjectTree Tree { get; init; } = null!;
            public Composition Composition { get; init; } = null!;
            public Layer Layer { get; init; } = null!;
            public TreeNode CompositionNode => Tree.Roots[0];
            public TreeNode LayerNode => CompositionNode.Children[0];
            public static TreeNode Group<T>(TreeNode node) where T : TreeGroup => node.Children.Single(child => child.Group is T);
        }

        // A composition with two layers. The first layer has two entities, one filter and three modifiers.
        private static Scene CreateScene()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.TextureModifierManager.AddItem(typeof(Blur));
            for (var i = 0; i < 3; i++)
                layer.ModifierManager.AddItem(typeof(TranslateModifier));

            return new Scene { Tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>()), Composition = composition, Layer = layer };
        }

        [Fact]
        public void ALayer_HoldsItsItemsByKind_InTheOrderOfItsRows()
        {
            var scene = CreateScene();

            Assert.Equal(
                new[] { new KindCount(TreeCategory.Entity, 2), new KindCount(TreeCategory.Filter, 1), new KindCount(TreeCategory.Modifier, 3) },
                scene.LayerNode.KindCounts);
            Assert.True(scene.LayerNode.HasKindCounts);
        }

        [Fact]
        public void ACompositionCountsItsLayersAsComposables()
        {
            var scene = CreateScene();

            Assert.Equal(new[] { new KindCount(TreeCategory.Composable, 2) }, scene.CompositionNode.KindCounts);
        }

        [Fact]
        public void AGroup_HoldsItsItemsOfItsOwnKind_AndAnEmptyGroupHoldsNothing()
        {
            var scene = CreateScene();

            Assert.Equal(new[] { new KindCount(TreeCategory.Entity, 2) }, Scene.Group<EntitiesGroup>(scene.LayerNode).KindCounts);
            Assert.Empty(Scene.Group<CamerasGroup>(scene.LayerNode).KindCounts);
            Assert.False(Scene.Group<CamerasGroup>(scene.LayerNode).HasKindCounts);
        }

        [Fact]
        public void AnEmptyRow_HoldsNothing()
        {
            var scene = CreateScene();
            var secondLayer = scene.CompositionNode.Children[1];

            Assert.Empty(secondLayer.KindCounts);
            Assert.False(secondLayer.HasKindCounts);
        }

        [Fact]
        public void TheCounts_FollowTheLists_AndTheParentOfAGroupTellsItsChange()
        {
            var scene = CreateScene();
            var changes = new List<string?>();
            scene.LayerNode.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            scene.Layer.CameraManager.AddItem(typeof(CMiX.Core.Rendering.Cameras.Camera));

            Assert.Contains(nameof(TreeNode.KindCounts), changes);
            Assert.Contains(new KindCount(TreeCategory.Camera, 1), scene.LayerNode.KindCounts);

            scene.Layer.ModelEntityManager.RemoveItem(scene.Layer.ModelEntityManager.ManagerData.Items[0]);
            Assert.Contains(new KindCount(TreeCategory.Entity, 1), scene.LayerNode.KindCounts);

            scene.Layer.ModelEntityManager.ClearAll();
            Assert.DoesNotContain(scene.LayerNode.KindCounts, count => count.Category == TreeCategory.Entity);
        }

        [Fact]
        public void ARowShowsItsCounts_OnlyWhileItIsCollapsedAndHoldsSomething()
        {
            var scene = CreateScene();
            var changes = new List<string?>();
            scene.LayerNode.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            Assert.False(scene.LayerNode.ShowsKindCounts);

            scene.LayerNode.IsExpanded = false;
            Assert.True(scene.LayerNode.ShowsKindCounts);
            Assert.Contains(nameof(TreeNode.ShowsKindCounts), changes);

            var empty = scene.CompositionNode.Children[1];
            empty.IsExpanded = false;
            Assert.False(empty.ShowsKindCounts);
        }

        [Fact]
        public void ALayerAddedToTheComposition_ChangesTheCountOfTheComposition()
        {
            var scene = CreateScene();
            var changes = new List<string?>();
            scene.CompositionNode.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            scene.Composition.LayerManager.AddItem(typeof(Layer));

            Assert.Contains(nameof(TreeNode.HasKindCounts), changes);
            Assert.Equal(new KindCount(TreeCategory.Composable, 3), scene.CompositionNode.KindCounts.Single());
        }

        [Fact]
        public void KindCounts_AfterAChildIsAdded_RecomputesOnce()
        {
            var scene = CreateScene();
            var group = Scene.Group<EntitiesGroup>(scene.LayerNode);
            var groupBefore = group.KindCountsComputations;
            var layerBefore = scene.LayerNode.KindCountsComputations;
            var compositionBefore = scene.CompositionNode.KindCountsComputations;

            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            _ = scene.LayerNode.KindCounts;
            _ = scene.LayerNode.HasKindCounts;
            _ = scene.LayerNode.ShowsKindCounts;

            Assert.Equal(groupBefore + 1, group.KindCountsComputations);
            Assert.Equal(layerBefore + 1, scene.LayerNode.KindCountsComputations);
            Assert.Equal(compositionBefore, scene.CompositionNode.KindCountsComputations);
            Assert.Contains(new KindCount(TreeCategory.Entity, 3), scene.LayerNode.KindCounts);
        }
    }
}
