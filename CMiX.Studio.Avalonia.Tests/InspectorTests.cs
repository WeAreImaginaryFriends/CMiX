using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Studio.Avalonia.ViewModels;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls;
using Xunit;
using CameraModel = CMiX.Core.Rendering.Cameras.Camera;
using CameraView = CMiX.Studio.Avalonia.Views.Camera;
using Composition = CMiX.Core.Compositing.Composition;
using CompositionView = CMiX.Studio.Avalonia.Views.Composition;
using FullscreenQuad = CMiX.Core.Compositing.FullscreenQuad;
using FullscreenQuadView = CMiX.Studio.Avalonia.Views.FullscreenQuad;
using Layer = CMiX.Core.Compositing.Layer;
using LightModel = CMiX.Core.Rendering.Lights.LightEntity;
using LightView = CMiX.Studio.Avalonia.Views.LightEntity;
using TextEntity = CMiX.Core.Compositing.TextEntity;
using TextEntityView = CMiX.Studio.Avalonia.Views.TextEntity;

namespace CMiX.Studio.Avalonia.Tests
{
    // The inspector shows the editor of the item that is selected in the project tree. The other tabs stay as they were.
    public class InspectorTests
    {
        private sealed class Screen
        {
            public MainWindow Window { get; init; } = null!;
            public MainViewModel ViewModel { get; init; } = null!;
            public Inspector Inspector { get; init; } = null!;
            public Layer Layer { get; init; } = null!;

            public ProjectTree Tree => ViewModel.ProjectTree;
            public TreeNode CompositionNode => Tree.Roots[0];
            public TreeNode LayerNode => CompositionNode.Children[0];
            public TreeNode Group(string header) => LayerNode.Children.Single(group => group.Header == header);

            // Found by its name: while no item is selected the scroll viewer is hidden and has no visual children yet.
            public ContentControl Editor => Inspector.FindControl<ContentControl>("editor")!;
            public ScrollViewer Scroller => Inspector.FindControl<ScrollViewer>("scroller")!;
            public TextBlock Title => Inspector.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "title");
            public TextBlock Placeholder => Inspector.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "placeholder");
            public Border Header => Inspector.GetVisualDescendants().OfType<Border>().First(b => b.Name == "header");
            public AppIcon HeaderIcon => Header.GetVisualDescendants().OfType<AppIcon>().First();

            public T? Find<T>() where T : class => Editor.GetVisualDescendants().OfType<T>().FirstOrDefault();
        }

        // A composition with a layer. The layer has one of each kind of item. The inspector tab is shown.
        private static Screen Show()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.SelectedItem;
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.SelectedItem;
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.AddItem(typeof(FullscreenQuad));
            layer.ModelEntityManager.AddItem(typeof(TextEntity));
            layer.CameraManager.AddItem(typeof(CameraModel));
            layer.LightManager.AddItem(typeof(LightModel));
            TestServiceProviderFactory.Pump();
            viewModel.ProjectTree.SelectedNode = null;
            TestServiceProviderFactory.SelectMainTab(window, "Inspector");
            var inspector = window.GetVisualDescendants().OfType<Inspector>().Single();
            TestServiceProviderFactory.Pump();
            return new Screen { Window = window, ViewModel = viewModel, Inspector = inspector, Layer = layer };
        }

        private static void Select(Screen screen, TreeNode node)
        {
            screen.Tree.SelectedNode = node;
            TestServiceProviderFactory.Pump();
        }

        [AvaloniaFact]
        public void TheInspectorTab_IsAfterTheSettings_AndTheOtherTabsAreStillThere()
        {
            var (_, window, _) = TestServiceProviderFactory.ShowMainWindow();
            var keys = TestServiceProviderFactory.MainTabControl(window).Items.OfType<TabItem>()
                .Select(item => (item.Header as AppIcon)?.IconKey).ToList();

            Assert.Equal("Settings", keys[0]);
            Assert.Equal("Inspector", keys[1]);
            Assert.Contains("Composition", keys);
            Assert.Contains("LayerScene", keys);
            Assert.Contains("Geometry", keys);
        }

        [AvaloniaFact]
        public void WithNothingSelected_TheInspectorShowsOnlyTheHint()
        {
            var screen = Show();
            Assert.True(screen.Placeholder.IsEffectivelyVisible);
            Assert.False(screen.Header.IsVisible);
            Assert.False(screen.Scroller.IsVisible);
        }

        [AvaloniaFact]
        public void ASelectedComposition_ShowsItsSettingsAndItsEditor()
        {
            var screen = Show();

            Select(screen, screen.CompositionNode);

            Assert.False(screen.Placeholder.IsVisible);
            Assert.Same(screen.CompositionNode.Item, screen.Editor.Content);
            Assert.NotNull(screen.Find<CompositionView>());
            Assert.Same(screen.CompositionNode.Item, screen.Find<ComposableItemSettings>()!.DataContext);
            Assert.Equal("Composition", screen.HeaderIcon.IconKey);
            Assert.Equal(screen.CompositionNode.Header, screen.Title.Text);
        }

        [AvaloniaFact]
        public void ASelectedLayer_ShowsItsSettingsAndItsEffects_ButNotTheListsOfItsItems()
        {
            var screen = Show();

            Select(screen, screen.LayerNode);

            Assert.Same(screen.Layer, screen.Editor.Content);
            Assert.Same(screen.Layer, screen.Find<ComposableItemSettings>()!.DataContext);
            Assert.NotNull(screen.Find<LayerPostFXTab>());
            Assert.Null(screen.Find<LayerEntityTab>());
            Assert.Null(screen.Find<LayerCameraTab>());
            Assert.Null(screen.Find<LayerLightTab>());
            Assert.Equal("LayerScene", screen.HeaderIcon.IconKey);
        }

        [AvaloniaTheory]
        [InlineData(0, typeof(ModelEntity))]
        [InlineData(1, typeof(FullscreenQuadView))]
        [InlineData(2, typeof(TextEntityView))]
        public void ASelectedEntity_ShowsTheEditorOfItsKind(int index, Type editor)
        {
            var screen = Show();
            var node = screen.Group("Entities").Children[index];

            Select(screen, node);

            Assert.Same(node.Item, screen.Editor.Content);
            var view = screen.Editor.GetVisualDescendants().OfType<Control>().First(visual => visual.GetType() == editor);
            Assert.Same(node.Item, view.DataContext);
        }

        [AvaloniaFact]
        public void ASelectedCamera_AndALight_ShowTheirEditors()
        {
            var screen = Show();

            Select(screen, screen.Group("Cameras").Children[0]);
            Assert.NotNull(screen.Find<CameraView>());
            Assert.Equal("Camera", screen.HeaderIcon.IconKey);

            Select(screen, screen.Group("Lights").Children[0]);
            Assert.NotNull(screen.Find<LightView>());
            Assert.Null(screen.Find<CameraView>());
        }

        [AvaloniaFact]
        public void AGroup_ShowsItsNameAndTheHint_AndNoEditor()
        {
            var screen = Show();

            Select(screen, screen.Group("Cameras"));

            Assert.True(screen.Header.IsVisible);
            Assert.Equal("Cameras", screen.Title.Text);
            Assert.True(screen.Placeholder.IsEffectivelyVisible);
            Assert.False(screen.Scroller.IsVisible);
        }

        [AvaloniaFact]
        public void TheInspector_FollowsTheSelection_AndTheNameOfTheItem()
        {
            var screen = Show();
            var entity = screen.Group("Entities").Children[0];

            Select(screen, screen.LayerNode);
            Assert.NotNull(screen.Find<LayerPostFXTab>());

            Select(screen, entity);
            Assert.Null(screen.Find<LayerPostFXTab>());
            Assert.NotNull(screen.Find<ModelEntity>());

            ((IPrefab)entity.Item!).PrefabService.Name.Value = "Floor";
            TestServiceProviderFactory.Pump();
            Assert.Equal("Floor", screen.Title.Text);
        }

        [AvaloniaFact]
        public void WhenTheSelectedItemIsRemoved_TheInspectorLeavesItsEditor()
        {
            var screen = Show();
            // The only camera: when it goes, the selection falls back to the group of cameras.
            var camera = screen.Group("Cameras").Children[0];
            Select(screen, camera);
            Assert.NotNull(screen.Find<CameraView>());

            screen.Layer.CameraManager.RemoveItem((IControl)camera.Item!);
            TestServiceProviderFactory.Pump();

            Assert.Null(screen.Find<CameraView>());
            Assert.Equal("Cameras", screen.Title.Text);
            Assert.True(screen.Placeholder.IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void ASelectionInAList_ShowsInTheInspectorToo()
        {
            var screen = Show();
            // A second camera is now the selected one in its list. Selecting the first one in the list moves the selection of the tree.
            screen.Layer.CameraManager.AddItem(typeof(CameraModel));
            TestServiceProviderFactory.Pump();
            var camera = screen.Group("Cameras").Children[0];

            screen.Layer.CameraManager.SelectAt(0);
            TestServiceProviderFactory.Pump();

            Assert.Same(camera.Item, screen.Editor.Content);
            Assert.NotNull(screen.Find<CameraView>());
        }
    }
}
