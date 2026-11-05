using CMiX.Core.Compositing;
using CMiX.Core.Modulation.Modifiers;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // During a drag, the rows that take the node inside are lit (candidates), the rows that it can go next to keep their look (neighbors,
    // the bar shows the place), and the other rows are dimmed. The rules are the ones of the drop itself.
    public class ProjectTreeDropTargetsTests
    {
        private sealed class Scene
        {
            public ProjectTree Tree { get; init; } = null!;
            public TreeNode CompositionA => Tree.Roots[0];
            public TreeNode CompositionB => Tree.Roots[1];
            public TreeNode Layer1 => CompositionA.Children[0];
            public TreeNode Layer2 => CompositionA.Children[1];
            public TreeNode LayerOfB => CompositionB.Children[0];
            public static TreeNode Group(TreeNode node, string header) => node.Children.Single(child => child.Header == header);
            public TreeNode Entity1 => Group(Layer1, "Entities").Children[0];
            public TreeNode Entity2 => Group(Layer1, "Entities").Children[1];
            public TreeNode EntityOfLayer2 => Group(Layer2, "Entities").Children[0];

            public IEnumerable<TreeNode> All() => Flatten(Tree.Roots);
            private static IEnumerable<TreeNode> Flatten(IEnumerable<TreeNode> nodes) =>
                nodes.SelectMany(node => new[] { node }.Concat(Flatten(node.Children)));
            public IEnumerable<TreeNode> Candidates() => All().Where(node => node.IsDropCandidate);
            public IEnumerable<TreeNode> Neighbors() => All().Where(node => node.IsDropNeighbor);
        }

        // Composition A: layer 1 has two entities, a camera, modifiers on the entities and two filters. Layer 2 has one entity and one
        // filter. B has one layer.
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
            var layer1 = (Layer)a.LayerManager.ManagerData.Items[0];
            var layer2 = (Layer)a.LayerManager.ManagerData.Items[1];
            layer1.ModelEntityManager.AddItem(typeof(Entity));
            layer1.ModelEntityManager.AddItem(typeof(Entity));
            layer1.CameraManager.AddItem(typeof(CMiX.Core.Rendering.Cameras.Camera));
            layer2.ModelEntityManager.AddItem(typeof(Entity));
            var first = (Entity)layer1.ModelEntityManager.ManagerData.Items[0];
            first.ModifierManager.AddItem(typeof(TranslateModifier));
            first.ModifierManager.AddItem(typeof(ScaleModifier));
            var second = (Entity)layer1.ModelEntityManager.ManagerData.Items[1];
            second.ModifierManager.AddItem(typeof(TranslateModifier));
            layer1.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));
            layer1.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));
            layer2.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));

            return new Scene { Tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>()) };
        }

        [Fact]
        public void BeforeADrag_NoRowHasADragState()
        {
            var scene = CreateScene();

            Assert.False(scene.Tree.IsDragging);
            Assert.All(scene.All(), node =>
            {
                Assert.False(node.IsDragActive);
                Assert.False(node.IsDropCandidate);
                Assert.False(node.IsDropNeighbor);
                Assert.False(node.IsDimmed);
            });
        }

        [Fact]
        public void DraggingAnEntity_LightsOnlyTheGroupsOfEntities_ItsOwnAndThoseOfOtherLayers()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            Assert.Equal(
                new[] { Scene.Group(scene.Layer1, "Entities"), Scene.Group(scene.Layer2, "Entities"), Scene.Group(scene.LayerOfB, "Entities") },
                scene.Candidates());
            Assert.True(scene.Tree.IsDragging);
        }

        [Fact]
        public void DraggingAnEntity_LeavesTheEntitiesNextToWhichItCanGoAsNeighbors_NotLit()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            Assert.Equal(new[] { scene.Entity2, scene.EntityOfLayer2 }, scene.Neighbors());
            Assert.All(scene.Neighbors(), node =>
            {
                Assert.False(node.IsDropCandidate);
                Assert.False(node.IsDimmed);
            });
        }

        // The case of a filter: only the groups of filters light up. The filters themselves keep their look, and the bar shows the place.
        [Fact]
        public void DraggingAFilter_LightsOnlyTheGroupsOfFilters_NotTheFilters()
        {
            var scene = CreateScene();
            var filters = Scene.Group(scene.Layer1, "Filters");
            var filter = filters.Children[0];

            scene.Tree.BeginDrag(filter);

            Assert.Equal(
                new[] { filters, Scene.Group(scene.Layer2, "Filters"), Scene.Group(scene.CompositionA, "Filters"), Scene.Group(scene.LayerOfB, "Filters"), Scene.Group(scene.CompositionB, "Filters") },
                scene.Candidates());
            Assert.All(scene.Candidates(), node => Assert.Null(node.Item));

            // The other filter of the list and the filter of the other layer: next to them, not inside.
            Assert.Equal(new[] { filters.Children[1], Scene.Group(scene.Layer2, "Filters").Children[0] }, scene.Neighbors());
        }

        [Fact]
        public void TheDraggedRow_IsNeverACandidate_AndItsOwnGroupIsOneUnlessTheRowIsAlreadyTheLast()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            Assert.True(scene.Entity1.IsDragSource);
            Assert.False(scene.Entity1.IsDropCandidate);
            Assert.False(scene.Entity1.IsDropNeighbor);
            Assert.True(Scene.Group(scene.Layer1, "Entities").IsDropCandidate);
            Assert.False(Scene.Group(scene.Layer1, "Cameras").IsDropCandidate);

            // The last entity of the list is at the end already: its own group has nothing to do for it.
            scene.Tree.EndDrag();
            scene.Tree.BeginDrag(scene.Entity2);

            Assert.False(Scene.Group(scene.Layer1, "Entities").IsDropCandidate);
            Assert.True(Scene.Group(scene.Layer2, "Entities").IsDropCandidate);
        }

        // The case of a row of modifiers: the group "Modifiers" of the entity takes it, and it goes to the end of the list.
        [Fact]
        public void ARowDroppedOnItsOwnGroup_GoesToTheEndOfItsList()
        {
            var scene = CreateScene();
            var service = new TreeDropService();
            var group = Scene.Group(scene.Entity1, "Modifiers");
            var first = group.Children[0];
            var second = group.Children[1];
            var firstItem = first.Item;

            Assert.True(service.CanDrop(first, group, DropPosition.Inside));
            Assert.False(service.CanDrop(second, group, DropPosition.Inside));

            var outcome = service.Drop(first, group, DropPosition.Inside);

            Assert.True(outcome.Moved);
            Assert.Same(firstItem, ((Entity)scene.Entity1.Item!).ModifierManager.ManagerData.Items[^1]);
            Assert.Equal(2, ((Entity)scene.Entity1.Item!).ModifierManager.ManagerData.Items.Count);
        }

        [Fact]
        public void DraggingALayer_LightsTheCompositions_AndLeavesTheOtherLayersAsNeighbors()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Layer1);

            Assert.Equal(new[] { scene.CompositionA, scene.CompositionB }, scene.Candidates());
            Assert.Equal(new[] { scene.Layer2, scene.LayerOfB }, scene.Neighbors());
        }

        [Fact]
        public void DraggingAModifier_LightsTheGroupsOfModifiers_ItsOwnAndThoseOfOtherEntities_AndNotOtherKinds()
        {
            var scene = CreateScene();
            var firstModifier = Scene.Group(scene.Entity1, "Modifiers").Children[0];
            var secondEntityGroup = Scene.Group(scene.Entity2, "Modifiers");

            scene.Tree.BeginDrag(firstModifier);

            Assert.Equal(
                new[] { Scene.Group(scene.Entity1, "Modifiers"), secondEntityGroup, Scene.Group(scene.EntityOfLayer2, "Modifiers") },
                scene.Candidates());
            Assert.Contains(Scene.Group(scene.Entity1, "Modifiers").Children[1], scene.Neighbors());
            Assert.Contains(secondEntityGroup.Children[0], scene.Neighbors());
            Assert.DoesNotContain(Scene.Group(scene.Layer1, "Filters"), scene.Candidates());
            Assert.All(scene.Candidates().Concat(scene.Neighbors()), node => Assert.Equal(TreeCategory.Modifier, node.Category));
        }

        [Fact]
        public void EveryRowThatIsNeitherLitNorANeighbor_IsDimmed_ExceptTheDraggedRowAndTheHints()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            foreach (var node in scene.All())
            {
                if (node.IsDropCandidate || node.IsDropNeighbor || node.IsDragSource || node.IsDropHint)
                    Assert.False(node.IsDimmed);
                else
                    Assert.True(node.IsDimmed, $"{node.Header} is not dimmed.");
            }
        }

        [Fact]
        public void ACollapsedRowWithATargetUnderIt_GivesAHint_AndAnOpenRowDoesNot()
        {
            var scene = CreateScene();
            scene.Layer2.IsExpanded = false;

            scene.Tree.BeginDrag(scene.Entity1);

            Assert.True(scene.Layer2.HasDropTargetBelow);
            Assert.True(scene.Layer2.IsDropHint);
            Assert.False(scene.Layer2.IsDimmed);
            Assert.False(scene.CompositionA.IsDropHint);

            scene.Layer2.IsExpanded = true;
            Assert.False(scene.Layer2.IsDropHint);
            Assert.True(scene.Layer2.IsDimmed);
        }

        [Fact]
        public void ARowWithNoTargetUnderIt_GivesNoHint()
        {
            var scene = CreateScene();
            scene.Tree.BeginDrag(scene.Entity1);

            var cameras = Scene.Group(scene.Layer1, "Cameras");
            cameras.IsExpanded = false;

            Assert.False(cameras.HasDropTargetBelow);
            Assert.False(cameras.IsDropHint);
        }

        [Fact]
        public void TheEndOfTheDrag_ClearsEveryState_AndTellsTheChanges()
        {
            var scene = CreateScene();
            var changes = new List<string?>();
            scene.Tree.BeginDrag(scene.Entity1);
            Scene.Group(scene.Layer2, "Entities").PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            scene.Tree.EndDrag();

            Assert.False(scene.Tree.IsDragging);
            Assert.All(scene.All(), node =>
            {
                Assert.False(node.IsDragActive);
                Assert.False(node.IsDragSource);
                Assert.False(node.IsDropCandidate);
                Assert.False(node.IsDropNeighbor);
                Assert.False(node.IsDimmed);
            });
            Assert.Contains(nameof(TreeNode.IsDropCandidate), changes);
            Assert.Contains(nameof(TreeNode.IsDimmed), changes);
        }

        [Fact]
        public void ADragThatStartsAfterAnother_StartsFromScratch()
        {
            var scene = CreateScene();
            scene.Tree.BeginDrag(scene.Entity1);
            scene.Tree.EndDrag();

            scene.Tree.BeginDrag(scene.Layer1);

            Assert.DoesNotContain(scene.Entity2, scene.Neighbors());
            Assert.Contains(scene.Layer2, scene.Neighbors());
        }

        [Fact]
        public void EveryLitRow_TakesTheDropInside_AndEveryNeighborTakesItBeforeOrAfter()
        {
            var scene = CreateScene();
            var service = new TreeDropService();
            var filter = Scene.Group(scene.Layer1, "Filters").Children[0];
            scene.Tree.BeginDrag(filter);

            foreach (var node in scene.All().Where(node => !ReferenceEquals(node, filter)))
            {
                if (node.IsDropCandidate)
                    Assert.True(service.CanDrop(filter, node, DropPosition.Inside), $"{node.Header} is lit but takes no drop inside.");

                if (node.IsDropNeighbor)
                    Assert.True(service.CanDrop(filter, node, DropPosition.Before) || service.CanDrop(filter, node, DropPosition.After), $"{node.Header} is a neighbor but takes no drop.");
            }
        }
    }
}
