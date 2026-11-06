using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tooltips of the tree show their text, and they open below the control like every tooltip of the app.
    // A data template for any object in a parent of a control turns the text of its tooltip into an empty box.
    public class ProjectTreeTooltipTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        // A composition with a layer that has two entities, a filter and three modifiers.
        private (ProjectTreeView View, ProjectTree Tree) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));
            for (var i = 0; i < 3; i++)
                layer.ModifierManager.AddItem(typeof(CMiX.Core.Modulation.Modifiers.TranslateModifier));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 600 };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            return (view, tree);
        }

        private void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window!.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        // Opens the tooltip of a control and returns the texts that it shows. The tooltip control is an internal value of the control,
        // so it is read by its property: the test needs what the user would see.
        private List<string> TextOfTooltip(Control control)
        {
            ToolTip.SetIsOpen(control, true);
            Settle();

            var property = (AvaloniaProperty)typeof(ToolTip).GetField("ToolTipProperty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
            var tip = (ToolTip)control.GetValue(property)!;
            var texts = tip.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? string.Empty).ToList();
            ToolTip.SetIsOpen(control, false);
            return texts;
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static List<Border> SquaresOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<KindCountBadges>().First()
                .GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("kindcount")).ToList();

        [AvaloniaFact]
        public void TheTooltipOfAMaskToggle_ShowsItsText_ForACompositionAndForALayer()
        {
            var (view, tree) = Show();

            foreach (var node in new[] { tree.Roots[0], tree.Roots[0].Children[0] })
            {
                var toggle = ItemOf(view, node).GetVisualDescendants().OfType<RowToggles>().First()
                    .GetVisualDescendants().OfType<ToggleButton>().Single(button => ToolTip.GetTip(button) != null);

                Assert.Equal(new[] { "Mask" }, TextOfTooltip(toggle));
            }
        }

        [AvaloniaFact]
        public void TheTooltipOfAMaskToggle_OpensBelowTheToggle_LikeEveryTooltip()
        {
            var (view, tree) = Show();
            var toggle = ItemOf(view, tree.Roots[0]).GetVisualDescendants().OfType<RowToggles>().First()
                .GetVisualDescendants().OfType<ToggleButton>().Single(button => ToolTip.GetTip(button) != null);

            Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(toggle));
        }

        [AvaloniaFact]
        public void TheTooltipOfACountSquare_SaysHowManyOfWhichKind()
        {
            var (view, tree) = Show();
            var layer = tree.Roots[0].Children[0];
            layer.IsExpanded = false;
            Settle();

            var texts = SquaresOf(view, layer).Select(square => TextOfTooltip(square).Single()).ToList();

            Assert.Equal(new[] { "2 Entities", "1 Filter", "3 Modifiers" }, texts);
        }

        [AvaloniaFact]
        public void TheTooltipOfTheCountOfAGroup_NamesItsKind()
        {
            var (view, tree) = Show();
            var entities = tree.Roots[0].Children[0].Children.Single(child => child.Group is EntitiesGroup);
            entities.IsExpanded = false;
            Settle();

            var square = Assert.Single(SquaresOf(view, entities));

            Assert.Equal("2 Entities", TextOfTooltip(square).Single());
            Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(square));
        }

        [Theory]
        [InlineData(TreeCategory.Composable, 1, "1 Composable")]
        [InlineData(TreeCategory.Composable, 4, "4 Composables")]
        [InlineData(TreeCategory.Entity, 1, "1 Entity")]
        [InlineData(TreeCategory.Entity, 2, "2 Entities")]
        [InlineData(TreeCategory.Camera, 1, "1 Camera")]
        [InlineData(TreeCategory.Camera, 3, "3 Cameras")]
        [InlineData(TreeCategory.Light, 1, "1 Light")]
        [InlineData(TreeCategory.Light, 2, "2 Lights")]
        [InlineData(TreeCategory.Texture, 1, "1 Texture")]
        [InlineData(TreeCategory.Texture, 2, "2 Textures")]
        [InlineData(TreeCategory.Filter, 1, "1 Filter")]
        [InlineData(TreeCategory.Filter, 5, "5 Filters")]
        [InlineData(TreeCategory.Modifier, 1, "1 Modifier")]
        [InlineData(TreeCategory.Modifier, 3, "3 Modifiers")]
        [InlineData(TreeCategory.Modulator, 1, "1 Modulator")]
        [InlineData(TreeCategory.Modulator, 2, "2 Modulators")]
        public void ADescription_IsTheNumberAndTheNameOfTheKind(TreeCategory category, int count, string expected)
        {
            Assert.Equal(expected, new KindCount(category, count).Description);
        }

        [Fact]
        public void OnlyACompositionAndALayer_HaveAMaskOwner()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var compositionNode = tree.Roots[0];
            var layerNode = compositionNode.Children[0];
            var entities = layerNode.Children.Single(child => child.Group is EntitiesGroup);

            Assert.Same(composition, compositionNode.MaskOwner);
            Assert.Same(layer, layerNode.MaskOwner);
            Assert.Null(entities.MaskOwner);
            Assert.Null(entities.Children[0].MaskOwner);
        }
    }
}
