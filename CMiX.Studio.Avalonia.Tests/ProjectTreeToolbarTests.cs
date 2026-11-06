using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Prefabs;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.Parts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Camera = CMiX.Core.Rendering.Cameras.Camera;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using LightEntity = CMiX.Core.Rendering.Lights.LightEntity;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The toolbar under the tree: add, duplicate, move up, move down and remove, for the selected node.
    // A button that has nothing to do for the selected node is disabled.
    public class ProjectTreeToolbarTests : IDisposable
    {
        private readonly List<Window> windows = new();

        public void Dispose()
        {
            foreach (var window in windows)
            {
                foreach (var button in window.GetVisualDescendants().OfType<Button>())
                    button.ContextMenu?.Close();

                window.Close();
            }
        }

        private sealed class Scene
        {
            public Project Project { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public Layer LayerOfA(int index) => (Layer)A.LayerManager.ManagerData.Items[index];
            public TreeNode LayerNodeOfA(int index) => Tree.Roots[0].Children[index];
            public TreeNode Group<T>(int layerIndex) where T : TreeGroup => LayerNodeOfA(layerIndex).Children.Single(group => group.Group is T);
            public TreeNode Group(int layerIndex, System.Type kind) => LayerNodeOfA(layerIndex).Children.Single(group => kind.IsInstanceOfType(group.Group));
        }

        // A holds two layers, B holds one.
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

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            return new Scene { Project = project, Tree = tree, A = a, B = b };
        }

        // The state of the five commands: add, duplicate, move up, move down, remove.
        private static bool[] Enabled(ProjectTree tree) => new[]
        {
            tree.AddItemCommand.CanExecute(null),
            tree.DuplicateItemCommand.CanExecute(null),
            tree.MoveItemUpCommand.CanExecute(null),
            tree.MoveItemDownCommand.CanExecute(null),
            tree.RemoveItemCommand.CanExecute(null)
        };

        [Fact]
        public void WithNothingSelected_OnlyAddIsEnabled_AndItAddsAComposition()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = null;

            Assert.Equal(new[] { true, false, false, false, false }, Enabled(scene.Tree));

            scene.Tree.AddItemCommand.Execute(scene.Tree.SelectedAdds.Single());

            Assert.Equal(3, scene.Project.CompositionManager.ManagerData.Items.Count);
            Assert.IsType<Composition>(scene.Project.CompositionManager.ManagerData.Items[2]);
        }

        [Fact]
        public void ACompositionSelected_AddsALayerToThatComposition()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = scene.Tree.Roots[1];

            // The toolbar opens a menu for a selected node. Its first row is the layer.
            scene.Tree.GetAddChoices(scene.Tree.SelectedNode).Single(choice => choice.Header == "Add Layer").Execute!.Invoke();

            Assert.Equal(2, scene.B.LayerManager.ManagerData.Items.Count);
            Assert.Equal(2, scene.A.LayerManager.ManagerData.Items.Count);
        }

        [Fact]
        public void TheFirstComposition_CannotMoveUp_AndTheLastCannotMoveDown()
        {
            var scene = CreateScene();

            scene.Tree.SelectedNode = scene.Tree.Roots[0];
            Assert.Equal(new[] { true, true, false, true, true }, Enabled(scene.Tree));

            scene.Tree.SelectedNode = scene.Tree.Roots[1];
            Assert.Equal(new[] { true, true, true, false, true }, Enabled(scene.Tree));
        }

        [Fact]
        public void ALayerSelected_HasFiveThingsToAdd_SoTheAddCommandAddsNothingItself()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = scene.LayerNodeOfA(0);

            var choices = scene.Tree.GetAddChoices(scene.Tree.SelectedNode);
            scene.Tree.AddItemCommand.Execute(null);

            Assert.Equal(new[] { "Entity", "Fullscreen Quad", "Text", "Camera", "Light", "Add Filter", "Add Modifier", "Add Composition" }, choices.Select(choice => choice.Header));
            Assert.True(scene.Tree.AddItemCommand.CanExecute(null));
            Assert.Empty(scene.LayerOfA(0).ModelEntityManager.ManagerData.Items);
            Assert.Empty(scene.LayerOfA(0).CameraManager.ManagerData.Items);
        }

        [Fact]
        public void TheEntitiesGroup_HasThreeThingsToAdd()
        {
            var scene = CreateScene();

            var choices = scene.Tree.GetAddChoices(scene.Group<EntitiesGroup>(0));

            Assert.Equal(new[] { "Add Entity", "Add Fullscreen Quad", "Add Text", "Add Composition" }, choices.Select(choice => choice.Header));
        }

        [Fact]
        public void TheCompositionIsTheLastChoiceOfAdd_WhateverIsSelected_AndTheOnlyOneWithNothingSelected()
        {
            var scene = CreateScene();
            scene.A.LayerManager.AddItem(typeof(Layer));
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            string[] Headers(CMiX.Studio.Avalonia.ViewModels.Tree.TreeNode? node) => scene.Tree.GetAddChoices(node).Select(choice => choice.Header).ToArray();

            Assert.Equal(new[] { "Add Composition" }, Headers(null));
            Assert.Equal(new[] { "Add Layer", "Add Filter", "Add Modifier", "Add Composition" }, Headers(scene.Tree.Roots[0]));
            Assert.Equal("Add Composition", Headers(scene.LayerNodeOfA(0)).Last());
            Assert.Equal(new[] { "Add Camera", "Add Composition" }, Headers(scene.Group<CamerasGroup>(0)));
            Assert.Equal(new[] { "Add Texture", "Add Modifier", "Add Composition" }, Headers(scene.Group<EntitiesGroup>(0).Children[0]));
        }

        [Fact]
        public void TheCamerasGroupAndTheLightsGroup_AddACameraAndALight()
        {
            var scene = CreateScene();
            var layer = scene.LayerOfA(0);

            // The toolbar opens a menu for a group, so the test takes the row of the kind from the choices.
            scene.Tree.GetAddChoices(scene.Group<CamerasGroup>(0)).Single(choice => choice.Header == "Add Camera").Execute!.Invoke();
            scene.Tree.GetAddChoices(scene.Group<LightsGroup>(0)).Single(choice => choice.Header == "Add Light").Execute!.Invoke();

            Assert.IsType<Camera>(Assert.Single(layer.CameraManager.ManagerData.Items));
            Assert.IsType<LightEntity>(Assert.Single(layer.LightManager.ManagerData.Items));
        }

        [Theory]
        [InlineData(typeof(EntitiesGroup))]
        [InlineData(typeof(CamerasGroup))]
        [InlineData(typeof(LightsGroup))]
        public void AGroupSelected_CanAdd_ButCannotDuplicateMoveOrRemove(System.Type group)
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = scene.Group(0, group);

            Assert.Equal(new[] { true, false, false, false, false }, Enabled(scene.Tree));
        }

        [Fact]
        public void AnEntitySelected_CanOnlyAddAComposition_AndCanDuplicateMoveAndRemove()
        {
            var scene = CreateScene();
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            var group = scene.Group<EntitiesGroup>(0);

            // Add is on: a composition can be added whatever is selected.
            scene.Tree.SelectedNode = group.Children[0];
            Assert.Equal(new[] { true, true, false, true, true }, Enabled(scene.Tree));

            scene.Tree.SelectedNode = group.Children[1];
            Assert.Equal(new[] { true, true, true, false, true }, Enabled(scene.Tree));
        }

        [Fact]
        public void Duplicate_CopiesTheSelectedLayerRightAfterIt()
        {
            var scene = CreateScene();
            var original = scene.LayerOfA(0);
            scene.Tree.SelectedNode = scene.LayerNodeOfA(0);

            scene.Tree.DuplicateItemCommand.Execute(null);

            Assert.Equal(3, scene.A.LayerManager.ManagerData.Items.Count);
            Assert.Same(original, scene.A.LayerManager.ManagerData.Items[0]);
            Assert.NotSame(original, scene.A.LayerManager.ManagerData.Items[1]);
        }

        [Fact]
        public void MoveDownAndMoveUp_MoveTheSelectedComposition()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = scene.Tree.Roots[0];

            scene.Tree.MoveItemDownCommand.Execute(null);
            Assert.Equal(new IControl[] { scene.B, scene.A }, scene.Project.CompositionManager.ManagerData.Items);
            Assert.Same(scene.A, scene.Tree.SelectedNode!.Item);

            scene.Tree.MoveItemUpCommand.Execute(null);
            Assert.Equal(new IControl[] { scene.A, scene.B }, scene.Project.CompositionManager.ManagerData.Items);
        }

        [Fact]
        public void Remove_TakesTheSelectedLayerOutOfItsComposition()
        {
            var scene = CreateScene();
            var second = scene.LayerOfA(1);
            scene.Tree.SelectedNode = scene.LayerNodeOfA(0);

            scene.Tree.RemoveItemCommand.Execute(null);

            Assert.Equal(new IControl[] { second }, scene.A.LayerManager.ManagerData.Items);
        }

        [Fact]
        public void TheCommands_TellThatTheyChanged_WhenTheSelectionChanges()
        {
            var scene = CreateScene();
            var raised = 0;
            scene.Tree.MoveItemDownCommand.CanExecuteChanged += (_, _) => raised++;

            scene.Tree.SelectedNode = scene.Tree.Roots[0];

            Assert.True(raised > 0);
        }

        [Fact]
        public void TheCommands_FollowTheListsOfTheTree()
        {
            var scene = CreateScene();
            scene.Tree.SelectedNode = scene.LayerNodeOfA(0);
            Assert.True(scene.Tree.MoveItemDownCommand.CanExecute(null));
            var raised = 0;
            scene.Tree.MoveItemDownCommand.CanExecuteChanged += (_, _) => raised++;

            // The second layer leaves: the selected layer is now the last one.
            scene.A.LayerManager.RemoveItem(scene.LayerOfA(1));

            Assert.False(scene.Tree.MoveItemDownCommand.CanExecute(null));
            Assert.True(raised > 0);
        }

        // The toolbar on screen.
        private sealed class Screen
        {
            public Window Window { get; init; } = null!;
            public ProjectTreeView View { get; init; } = null!;
            public Scene Scene { get; init; } = null!;

            public Button ButtonOf(string name) => View.GetVisualDescendants().OfType<Button>().First(button => button.Name == name);

            // A click on a row of the menu.
            public void ClickRow(ContextMenu menu, string header)
            {
                menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, header)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Dispatcher.UIThread.RunJobs();
            }

            public void Click(Control control)
            {
                Window.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;
                Window.MouseDown(point, MouseButton.Left);
                Window.MouseUp(point, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
            }
        }

        private Screen Show(double leftRoom = 0)
        {
            var scene = CreateScene();
            var view = new ProjectTreeView { DataContext = scene.Tree, Margin = new Thickness(leftRoom, 0, 0, 0) };
            var window = new Window { Content = view, Width = 400, Height = 900 };
            windows.Add(window);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return new Screen { Window = window, View = view, Scene = scene };
        }

        [AvaloniaFact]
        public void TheToolbar_IsUnderTheTree_WithTheFiveButtonsOfTheListToolbars()
        {
            var screen = Show();
            var tree = screen.View.GetVisualDescendants().OfType<TreeView>().First();
            var buttons = new[] { "AddButton", "DuplicateButton", "MoveUpButton", "MoveDownButton", "RemoveButton" }.Select(screen.ButtonOf).ToList();
            var height = Convert.ToDouble(Application.Current!.FindResource("ControlHeightMD"));

            Assert.All(buttons, button => Assert.Equal(height, button.Bounds.Height));
            var treeBottom = tree.TranslatePoint(new Point(0, tree.Bounds.Height), screen.Window)!.Value.Y;
            Assert.All(buttons, button => Assert.True(button.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y >= treeBottom - 1));
        }

        [AvaloniaFact]
        public void TheButtons_AreDisabledWhenTheSelectedNodeHasNothingToDo()
        {
            var screen = Show();
            var entityParent = screen.Scene.LayerOfA(0);
            entityParent.ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();

            screen.Scene.Tree.SelectedNode = screen.Scene.Group<EntitiesGroup>(0).Children[0];
            // A composition can be added whatever is selected, so Add is never disabled.
            Assert.True(screen.ButtonOf("AddButton").IsEffectivelyEnabled);
            Assert.True(screen.ButtonOf("DuplicateButton").IsEffectivelyEnabled);
            Assert.False(screen.ButtonOf("MoveUpButton").IsEffectivelyEnabled);

            screen.Scene.Tree.SelectedNode = screen.Scene.Group<EntitiesGroup>(0);
            Assert.True(screen.ButtonOf("AddButton").IsEffectivelyEnabled);
            Assert.False(screen.ButtonOf("DuplicateButton").IsEffectivelyEnabled);
            Assert.False(screen.ButtonOf("RemoveButton").IsEffectivelyEnabled);
        }

        [AvaloniaFact]
        public void AClickOnAdd_WithACompositionSelected_OpensAMenuWithAddLayerAndAddComposition()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.Tree.Roots[1];

            screen.Click(screen.ButtonOf("AddButton"));

            var menu = screen.ButtonOf("AddButton").ContextMenu!;
            var headers = menu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList();
            Assert.Equal(new[] { screen.Scene.B.PrefabService.Name.Value, "-", "Add Layer", "Add Filter", "Add Modifier", "Add Composition" }, headers);
            Assert.Equal(1, screen.Scene.B.LayerManager.ManagerData.Items.Count);
            Assert.Equal(2, screen.Scene.Project.CompositionManager.ManagerData.Items.Count);

            screen.ClickRow(menu, "Add Composition");

            Assert.Equal(3, screen.Scene.Project.CompositionManager.ManagerData.Items.Count);
            Assert.Equal(1, screen.Scene.B.LayerManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void TheAddLayerRowOfTheMenu_AddsALayerToTheSelectedComposition()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.Tree.Roots[1];
            screen.Click(screen.ButtonOf("AddButton"));

            screen.ClickRow(screen.ButtonOf("AddButton").ContextMenu!, "Add Layer");

            Assert.Equal(2, screen.Scene.B.LayerManager.ManagerData.Items.Count);
            Assert.Equal(2, screen.Scene.Project.CompositionManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void AClickOnAdd_WithAnEntitySelected_OffersTheCompositionOnly_AndAddsNothingByItself()
        {
            var screen = Show();
            screen.Scene.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();
            screen.Scene.Tree.SelectedNode = screen.Scene.Group<EntitiesGroup>(0).Children[0];

            screen.Click(screen.ButtonOf("AddButton"));

            var menu = screen.ButtonOf("AddButton").ContextMenu!;
            Assert.True(menu.IsOpen);
            Assert.Equal(new[] { "Add Texture", "Add Modifier", "Add Composition" }, menu.Items.OfType<MenuItem>().Skip(2).Select(item => item.Header?.ToString()));
            Assert.Equal(2, screen.Scene.Project.CompositionManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void AClickOnAdd_WithACamerasGroupSelected_OpensAMenu_AndAddACameraFromIt()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.Group<CamerasGroup>(0);

            screen.Click(screen.ButtonOf("AddButton"));
            screen.ClickRow(screen.ButtonOf("AddButton").ContextMenu!, "Add Camera");

            Assert.IsType<Camera>(Assert.Single(screen.Scene.LayerOfA(0).CameraManager.ManagerData.Items));
        }

        [AvaloniaFact]
        public void AClickOnAdd_WithALayerSelected_OpensAMenuOfTheFiveThingsToAddAndTheComposition()
        {
            var screen = Show();
            var layer = screen.Scene.LayerNodeOfA(0);
            screen.Scene.Tree.SelectedNode = layer;

            screen.Click(screen.ButtonOf("AddButton"));

            var menu = screen.ButtonOf("AddButton").ContextMenu!;
            Assert.True(menu.IsOpen);
            var headers = menu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList();
            Assert.Equal(new[] { layer.Header, "-", "Entity", "Fullscreen Quad", "Text", "Camera", "Light", "Add Filter", "Add Modifier", "Add Composition" }, headers);
            Assert.Empty(screen.Scene.LayerOfA(0).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void TheMenuOfAdd_StandsOnTheButton_EightPixelsOverItsTopLeftCorner()
        {
            // The room on the left keeps the popup inside the window of the test. A real popup is a window of its own.
            var screen = Show(leftRoom: 100);
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(0);
            var button = screen.ButtonOf("AddButton");

            screen.Click(button);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();

            var menu = button.ContextMenu!;
            var frame = menu.GetVisualDescendants().OfType<PopupFrame>().First();
            var frameTopLeft = frame.TranslatePoint(new Point(0, 0), screen.Window)!.Value;
            var buttonTopLeft = button.TranslatePoint(new Point(0, 0), screen.Window)!.Value;

            // The frame is larger than the panel by the margin of the shadow.
            var panelLeft = frameTopLeft.X + PopupFrame.ShadowMargin.Left;
            var panelBottom = frameTopLeft.Y + frame.Bounds.Height - PopupFrame.ShadowMargin.Bottom;
            Assert.InRange(panelLeft, buttonTopLeft.X - 0.5, buttonTopLeft.X + 0.5);
            Assert.InRange(panelBottom, buttonTopLeft.Y - 8.5, buttonTopLeft.Y - 7.5);
        }

        [AvaloniaFact]
        public void TheMenuOfAdd_StaysOverTheButton_WhateverTheNumberOfRows()
        {
            var screen = Show(leftRoom: 100);
            var button = screen.ButtonOf("AddButton");
            var buttonTop = button.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y;

            foreach (var node in new[] { screen.Scene.Group<EntitiesGroup>(0), screen.Scene.LayerNodeOfA(0) })
            {
                screen.Scene.Tree.SelectedNode = node;
                screen.Click(button);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();

                var frame = button.ContextMenu!.GetVisualDescendants().OfType<PopupFrame>().First();
                var panelBottom = frame.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y + frame.Bounds.Height - PopupFrame.ShadowMargin.Bottom;
                Assert.InRange(panelBottom, buttonTop - 8.5, buttonTop - 7.5);

                button.ContextMenu.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }

        [AvaloniaFact]
        public void AChoiceOfTheMenu_AddsThatThingToTheSelectedLayer()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(1);
            screen.Click(screen.ButtonOf("AddButton"));
            var menu = screen.ButtonOf("AddButton").ContextMenu!;

            menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Light"))
                .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<LightEntity>(Assert.Single(screen.Scene.LayerOfA(1).LightManager.ManagerData.Items));
            Assert.Empty(screen.Scene.LayerOfA(0).LightManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void AClickOnAdd_WithTheEntitiesGroupSelected_OpensAMenuOfTheThreeKindsOfEntity()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.Group<EntitiesGroup>(0);

            screen.Click(screen.ButtonOf("AddButton"));

            var menu = screen.ButtonOf("AddButton").ContextMenu!;
            var headers = menu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).ToList();
            Assert.Equal(new[] { "Entities", "-", "Add Entity", "Add Fullscreen Quad", "Add Text", "Add Composition" }, headers);
        }

        [AvaloniaFact]
        public void TheOtherButtons_ActOnTheSelectedNode()
        {
            var screen = Show();
            var first = screen.Scene.LayerOfA(0);
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(0);

            screen.Click(screen.ButtonOf("DuplicateButton"));
            Assert.Equal(3, screen.Scene.A.LayerManager.ManagerData.Items.Count);

            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(0);
            screen.Click(screen.ButtonOf("MoveDownButton"));
            Assert.NotSame(first, screen.Scene.A.LayerManager.ManagerData.Items[0]);

            screen.Click(screen.ButtonOf("RemoveButton"));
            Assert.Equal(2, screen.Scene.A.LayerManager.ManagerData.Items.Count);
        }
    }
}
