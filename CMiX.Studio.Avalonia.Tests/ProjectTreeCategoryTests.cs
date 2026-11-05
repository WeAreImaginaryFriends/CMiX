using CMiX.Core.Compositing;
using CMiX.Core.Modulation.Modifiers;
using CMiX.Core.Modulation.Modulators;
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
    // A node of the tree knows its kind, and how many composables are around it.
    public class ProjectTreeCategoryTests
    {
        private static ProjectTree CreateTree()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.TextureModifierManager.AddItem(typeof(Blur));
            var entity = (Entity)layer.ModelEntityManager.ManagerData.Items[0];
            entity.ModifierManager.AddItem(typeof(TranslateModifier));
            ((CMiX.Core.Modulation.IModulatable)entity.ModifierManager.ManagerData.Items[0]).ModulatorManager.AddItem(typeof(LFOModulator));
            return new ProjectTree(project, provider.GetRequiredService<UndoManager>());
        }

        private static TreeNode Child(TreeNode node, string header) => node.Children.Single(child => child.Header == header);

        [Fact]
        public void EveryRow_HasTheKindOfItsItem_AndAGroupHasTheKindOfItsItems()
        {
            var composition = CreateTree().Roots[0];
            var layer = composition.Children[0];
            var entities = Child(layer, "Entities");
            var entity = entities.Children[0];
            var modifier = Child(entity, "Modifiers").Children[0];

            Assert.Equal(TreeCategory.Composable, composition.Category);
            Assert.Equal(TreeCategory.Composable, layer.Category);
            Assert.Equal(TreeCategory.Entity, entities.Category);
            Assert.Equal(TreeCategory.Entity, entity.Category);
            Assert.Equal(TreeCategory.Camera, Child(layer, "Cameras").Category);
            Assert.Equal(TreeCategory.Light, Child(layer, "Lights").Category);
            Assert.Equal(TreeCategory.Filter, Child(layer, "Filters").Category);
            Assert.Equal(TreeCategory.Filter, Child(layer, "Filters").Children[0].Category);
            Assert.Equal(TreeCategory.Texture, Child(entity, "Textures").Category);
            Assert.Equal(TreeCategory.Modifier, Child(entity, "Modifiers").Category);
            Assert.Equal(TreeCategory.Modifier, modifier.Category);
            Assert.Equal(TreeCategory.Modulator, Child(modifier, "Modulators").Category);
            Assert.Equal(TreeCategory.Modulator, Child(modifier, "Modulators").Children[0].Category);
        }

        [Fact]
        public void TheComposableDepth_CountsTheComposablesAroundTheRow()
        {
            var composition = CreateTree().Roots[0];
            var layer = composition.Children[0];
            var entity = Child(layer, "Entities").Children[0];

            Assert.Equal(0, composition.ComposableDepth);
            Assert.Equal(1, layer.ComposableDepth);
            Assert.Equal(2, Child(layer, "Entities").ComposableDepth);
            Assert.Equal(2, entity.ComposableDepth);
        }

        [Fact]
        public void TheKindDepth_CountsTheChangesOfKindDownToTheRow()
        {
            var composition = CreateTree().Roots[0];
            var layer = composition.Children[0];
            var entities = Child(layer, "Entities");
            var entity = entities.Children[0];
            var modifiers = Child(entity, "Modifiers");
            var modifier = modifiers.Children[0];
            var modulators = Child(modifier, "Modulators");

            Assert.Equal(0, composition.KindDepth);
            Assert.Equal(0, layer.KindDepth);
            Assert.Equal(1, entities.KindDepth);
            Assert.Equal(1, entity.KindDepth);
            Assert.Equal(2, modifiers.KindDepth);
            Assert.Equal(2, modifier.KindDepth);
            Assert.Equal(3, modulators.KindDepth);
            Assert.Equal(3, modulators.Children[0].KindDepth);
            Assert.Equal(1, Child(layer, "Filters").KindDepth);
            Assert.Equal(1, Child(layer, "Filters").Children[0].KindDepth);
        }

        [Fact]
        public void ALayerAddedLater_HasTheKindAndTheDepthOfALayer()
        {
            var tree = CreateTree();
            var composition = tree.Roots[0];
            var source = (Composition)composition.Item!;

            source.LayerManager.AddItem(typeof(Layer));

            var added = composition.Children.Where(child => child.Item is Layer).Last();
            Assert.Equal(TreeCategory.Composable, added.Category);
            Assert.Equal(1, added.ComposableDepth);
        }
    }
}
