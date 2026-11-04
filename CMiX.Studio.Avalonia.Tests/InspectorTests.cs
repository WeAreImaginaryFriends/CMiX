using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Studio.Avalonia.ViewModels;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls;
using CMiX.Core.Undo;
using Microsoft.Extensions.DependencyInjection;
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
            public UndoManager UndoManager { get; init; } = null!;

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
            var (provider, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
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
            return new Screen { Window = window, ViewModel = viewModel, Inspector = inspector, Layer = layer, UndoManager = provider.GetRequiredService<UndoManager>() };
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
        public void AGroup_ShowsItsNameAndTheListPanelOfItsKind()
        {
            var screen = Show();

            Select(screen, screen.Group("Cameras"));
            Assert.True(screen.Header.IsVisible);
            Assert.Equal("Cameras", screen.Title.Text);
            Assert.False(screen.Placeholder.IsVisible);
            Assert.True(screen.Scroller.IsVisible);
            Assert.Same(screen.Layer, Assert.IsType<CamerasGroup>(screen.Editor.Content).Layer);
            Assert.Same(screen.Layer, screen.Find<LayerCameraTab>()!.DataContext);
            Assert.Null(screen.Find<LayerEntityTab>());

            Select(screen, screen.Group("Entities"));
            Assert.Equal("Entities", screen.Title.Text);
            Assert.Same(screen.Layer, screen.Find<LayerEntityTab>()!.DataContext);
            Assert.Null(screen.Find<LayerCameraTab>());

            Select(screen, screen.Group("Lights"));
            Assert.Equal("Lights", screen.Title.Text);
            Assert.Same(screen.Layer, screen.Find<LayerLightTab>()!.DataContext);
            Assert.Null(screen.Find<LayerEntityTab>());
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

            // The selection falls back to the group of cameras, which shows the panel of the list.
            Assert.Equal("Cameras", screen.Title.Text);
            Assert.IsType<CamerasGroup>(screen.Editor.Content);
            Assert.NotNull(screen.Find<LayerCameraTab>());
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

        // Every panel of an editor is open when the item is new.
        [AvaloniaTheory]
        [InlineData("composition")]
        [InlineData("layer")]
        [InlineData("entity")]
        [InlineData("quad")]
        [InlineData("text")]
        [InlineData("camera")]
        [InlineData("light")]
        [InlineData("entities group")]
        [InlineData("cameras group")]
        [InlineData("lights group")]
        public void EveryPanelOfAnEditor_IsOpen(string kind)
        {
            var screen = Show();
            var node = kind switch
            {
                "composition" => screen.CompositionNode,
                "layer" => screen.LayerNode,
                "entity" => screen.Group("Entities").Children[0],
                "quad" => screen.Group("Entities").Children[1],
                "text" => screen.Group("Entities").Children[2],
                "camera" => screen.Group("Cameras").Children[0],
                "entities group" => screen.Group("Entities"),
                "cameras group" => screen.Group("Cameras"),
                "lights group" => screen.Group("Lights"),
                _ => screen.Group("Lights").Children[0]
            };

            Select(screen, node);

            var expanders = screen.Editor.GetVisualDescendants().OfType<Expander>().ToList();
            Assert.NotEmpty(expanders);
            Assert.All(expanders, expander => Assert.True(expander.IsExpanded, "Closed panel: " + expander.Header));
        }

        // ---- the inspector follows what happens to the item ----

        [AvaloniaFact]
        public async Task AnEntityMovedToAnotherLayer_StaysInTheInspector()
        {
            var screen = Show();
            var composition = (Composition)screen.CompositionNode.Item!;
            composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();
            var entity = screen.Group("Entities").Children[0];
            var item = entity.Item;
            Select(screen, entity);
            var otherEntities = screen.CompositionNode.Children[1].Children.Single(group => group.Header == "Entities");

            await screen.Tree.DropAsync(entity, otherEntities, DropPosition.Inside);
            TestServiceProviderFactory.Pump();

            Assert.Same(item, screen.Editor.Content);
            Assert.NotNull(screen.Find<ModelEntity>());
            Assert.Same(item, screen.Tree.SelectedNode!.Item);
        }

        [AvaloniaFact]
        public void WhenTheCompositionOfTheSelectedLayerIsRemoved_TheInspectorShowsTheHint()
        {
            var screen = Show();
            Select(screen, screen.LayerNode);
            Assert.NotNull(screen.Find<LayerPostFXTab>());

            screen.ViewModel.Project.CompositionManager.RemoveItem((IControl)screen.CompositionNode.Item!);
            TestServiceProviderFactory.Pump();

            Assert.Null(screen.Tree.SelectedNode);
            Assert.True(screen.Placeholder.IsEffectivelyVisible);
            Assert.False(screen.Header.IsVisible);
            Assert.False(screen.Scroller.IsVisible);
        }

        [AvaloniaFact]
        public void AnUndoOfTheRemoval_AndARedo_LeaveTheInspectorWorking()
        {
            var screen = Show();
            var camera = screen.Group("Cameras").Children[0];
            Select(screen, camera);

            screen.Layer.CameraManager.RemoveItem((IControl)camera.Item!);
            TestServiceProviderFactory.Pump();
            Assert.Equal("Cameras", screen.Title.Text);

            screen.UndoManager.Undo();
            TestServiceProviderFactory.Pump();
            screen.UndoManager.Redo();
            TestServiceProviderFactory.Pump();

            Assert.Equal("Cameras", screen.Title.Text);
            Assert.IsType<CamerasGroup>(screen.Editor.Content);
        }

        [AvaloniaFact]
        public void AfterAnotherTab_TheInspectorShowsTheCurrentSelection()
        {
            var screen = Show();
            Select(screen, screen.LayerNode);

            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");
            Select(screen, screen.Group("Cameras").Children[0]);
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Inspector");

            var inspector = screen.Window.GetVisualDescendants().OfType<Inspector>().Single();
            Assert.NotNull(inspector.GetVisualDescendants().OfType<CameraView>().FirstOrDefault());
            Assert.Null(inspector.GetVisualDescendants().OfType<LayerPostFXTab>().FirstOrDefault());
        }

        // ---- the inspector tab opens on a click in the tree ----

        private static Point CenterOfRow(Screen screen, TreeNode node)
        {
            var tree = screen.Window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            screen.Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var item = tree.GetVisualDescendants().OfType<TreeViewItem>().First(row => ReferenceEquals(row.DataContext, node));
            var rowBorder = item.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            // Right of the name and left of the buttons: on the row, on nothing else.
            return rowBorder.TranslatePoint(new Point(rowBorder.Bounds.Width * 0.5, rowBorder.Bounds.Height / 2), screen.Window)!.Value;
        }

        private static void Click(Screen screen, Point point)
        {
            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            TestServiceProviderFactory.Pump();
        }

        private static string SelectedTab(Screen screen) =>
            (TestServiceProviderFactory.MainTabControl(screen.Window).SelectedItem as TabItem is { Header: AppIcon icon }) ? icon.IconKey : "";

        [AvaloniaFact]
        public void AClickOnARow_ShowsTheInspectorWithThatItem()
        {
            var screen = Show();
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");

            Click(screen, CenterOfRow(screen, screen.Group("Cameras").Children[0]));

            Assert.Equal("Inspector", SelectedTab(screen));
            Assert.NotNull(screen.Find<CameraView>());
        }

        [AvaloniaFact]
        public void AClickOnAGroup_ShowsTheInspectorToo()
        {
            var screen = Show();
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");

            Click(screen, CenterOfRow(screen, screen.Group("Lights")));

            Assert.Equal("Inspector", SelectedTab(screen));
            Assert.Equal("Lights", screen.Title.Text);
        }

        [AvaloniaFact]
        public void AClickOnTheVisibilityButton_DoesNotChangeTheTab()
        {
            var screen = Show();
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");
            var tree = screen.Window.GetVisualDescendants().OfType<ProjectTreeView>().Single();
            var item = tree.GetVisualDescendants().OfType<TreeViewItem>().First(row => ReferenceEquals(row.DataContext, screen.LayerNode));
            var toggle = item.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder")
                .GetVisualDescendants().OfType<ToggleButton>().First(button => CMiX.UI.Behaviors.ToggleDrag.GetGroup(button) == "visibility");
            var point = toggle.TranslatePoint(new Point(toggle.Bounds.Width / 2, toggle.Bounds.Height / 2), screen.Window)!.Value;

            Click(screen, point);

            Assert.Equal("Settings", SelectedTab(screen));
        }

        [AvaloniaFact]
        public void ASelectionByAProgramOrAList_DoesNotChangeTheTab()
        {
            var screen = Show();
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");

            Select(screen, screen.LayerNode);
            Assert.Equal("Settings", SelectedTab(screen));

            screen.Layer.CameraManager.AddItem(typeof(CameraModel));
            TestServiceProviderFactory.Pump();
            screen.Layer.CameraManager.SelectAt(0);
            TestServiceProviderFactory.Pump();

            Assert.Equal("Settings", SelectedTab(screen));
        }

        [AvaloniaFact]
        public void WhenTheOptionIsOff_AClickDoesNotChangeTheTab()
        {
            var screen = Show();
            screen.Window.OpenInspectorOnTreeClick = false;
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");

            Click(screen, CenterOfRow(screen, screen.LayerNode));

            Assert.Equal("Settings", SelectedTab(screen));
            Assert.Same(screen.Layer, screen.Tree.SelectedNode!.Item);
        }
    }
}
