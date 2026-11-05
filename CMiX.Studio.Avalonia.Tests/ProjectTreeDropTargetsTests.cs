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
    // During a drag, every row that can take the node is a candidate. The rules are the ones of the drop itself.
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
        }

        // Composition A: layer 1 has two entities, a camera and modifiers on the first entity. Layer 2 has one entity. B has one layer.
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
                Assert.False(node.IsDimmed);
            });
        }

        [Fact]
        public void DraggingAnEntity_MakesCandidatesOfTheRowsThatCanTakeIt()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            // The other entity of its list, the group of entities of another layer and the entities in it.
            Assert.Equal(
                new[] { scene.Entity2, Scene.Group(scene.Layer2, "Entities"), scene.EntityOfLayer2, Scene.Group(scene.LayerOfB, "Entities") },
                scene.Candidates());
            Assert.True(scene.Tree.IsDragging);
        }

        [Fact]
        public void TheDraggedRow_AndItsOwnGroup_AreNeverCandidates()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            Assert.True(scene.Entity1.IsDragSource);
            Assert.False(scene.Entity1.IsDropCandidate);
            Assert.False(Scene.Group(scene.Layer1, "Entities").IsDropCandidate);
            Assert.False(Scene.Group(scene.Layer1, "Cameras").IsDropCandidate);
        }

        [Fact]
        public void DraggingALayer_MakesCandidatesOfTheOtherLayersAndOfEveryComposition()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Layer1);

            Assert.Equal(
                new[] { scene.CompositionA, scene.Layer2, scene.CompositionB, scene.LayerOfB },
                scene.Candidates());
        }

        [Fact]
        public void DraggingAModifier_MakesCandidatesOfTheModifiersOfOtherEntities_AndNotOfOtherKinds()
        {
            var scene = CreateScene();
            var firstModifier = Scene.Group(scene.Entity1, "Modifiers").Children[0];
            var secondEntityGroup = Scene.Group(scene.Entity2, "Modifiers");

            scene.Tree.BeginDrag(firstModifier);

            Assert.Contains(Scene.Group(scene.Entity1, "Modifiers").Children[1], scene.Candidates());
            Assert.Contains(secondEntityGroup, scene.Candidates());
            Assert.Contains(secondEntityGroup.Children[0], scene.Candidates());
            Assert.DoesNotContain(Scene.Group(scene.Layer1, "Filters"), scene.Candidates());
            Assert.DoesNotContain(Scene.Group(scene.Entity1, "Modifiers"), scene.Candidates());
            Assert.All(scene.Candidates(), node => Assert.Equal(TreeCategory.Modifier, node.Category));
        }

        [Fact]
        public void EveryRowThatIsNotACandidate_IsDimmed_ExceptTheDraggedRowAndTheHints()
        {
            var scene = CreateScene();

            scene.Tree.BeginDrag(scene.Entity1);

            foreach (var node in scene.All())
            {
                if (node.IsDropCandidate || node.IsDragSource || node.IsDropHint)
                    Assert.False(node.IsDimmed);
                else
                    Assert.True(node.IsDimmed, $"{node.Header} is not dimmed.");
            }
        }

        [Fact]
        public void ACollapsedRowWithACandidateUnderIt_GivesAHint_AndAnOpenRowDoesNot()
        {
            var scene = CreateScene();
            scene.Layer2.IsExpanded = false;

            scene.Tree.BeginDrag(scene.Entity1);

            Assert.True(scene.Layer2.HasDropCandidateBelow);
            Assert.True(scene.Layer2.IsDropHint);
            Assert.False(scene.Layer2.IsDimmed);
            Assert.False(scene.CompositionA.IsDropHint);

            scene.Layer2.IsExpanded = true;
            Assert.False(scene.Layer2.IsDropHint);
            Assert.True(scene.Layer2.IsDimmed);
        }

        [Fact]
        public void ARowWithNoCandidateUnderIt_GivesNoHint()
        {
            var scene = CreateScene();
            scene.Tree.BeginDrag(scene.Entity1);

            var cameras = Scene.Group(scene.Layer1, "Cameras");
            cameras.IsExpanded = false;

            Assert.False(cameras.HasDropCandidateBelow);
            Assert.False(cameras.IsDropHint);
        }

        [Fact]
        public void TheEndOfTheDrag_ClearsEveryState_AndTellsTheChanges()
        {
            var scene = CreateScene();
            var changes = new List<string?>();
            scene.Tree.BeginDrag(scene.Entity1);
            scene.Entity2.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            scene.Tree.EndDrag();

            Assert.False(scene.Tree.IsDragging);
            Assert.All(scene.All(), node =>
            {
                Assert.False(node.IsDragActive);
                Assert.False(node.IsDragSource);
                Assert.False(node.IsDropCandidate);
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

            Assert.DoesNotContain(scene.Entity2, scene.Candidates());
            Assert.Contains(scene.Layer2, scene.Candidates());
        }

        [Fact]
        public void TheCandidates_AreTheRowsThatTheDropCanUse()
        {
            var scene = CreateScene();
            var service = new TreeDropService();
            scene.Tree.BeginDrag(scene.Entity1);

            foreach (var node in scene.All().Where(node => !ReferenceEquals(node, scene.Entity1)))
            {
                var canDrop = new[] { DropPosition.Before, DropPosition.After, DropPosition.Inside }.Any(position => service.CanDrop(scene.Entity1, node, position));
                if (node.IsDropCandidate)
                    Assert.True(canDrop, $"{node.Header} is a candidate but takes no drop.");
            }
        }
    }
}
