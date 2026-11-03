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
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.String;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Camera = CMiX.Core.Rendering.Cameras.Camera;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using FullscreenQuad = CMiX.Core.Compositing.FullscreenQuad;
using Layer = CMiX.Core.Compositing.Layer;
using LightEntity = CMiX.Core.Rendering.Lights.LightEntity;
using Project = CMiX.Core.Compositing.Project;
using TextEntity = CMiX.Core.Compositing.TextEntity;

namespace CMiX.Studio.Avalonia.Tests
{
    // The menu of a row of the project tree: the rows of the menus of the lists, and the rows that add.
    public class TreeRowMenuTests : IDisposable
    {
        private readonly List<Screen> screens = new();

        // The menus and the windows of a test are closed, so an open popup never reaches the next test.
        public void Dispose()
        {
            foreach (var screen in screens)
            {
                screen.TreeView.ContextMenu?.Close();
                screen.Window.Close();
            }
        }

        private sealed class Screen
        {
            public Window Window { get; init; } = null!;
            public ProjectTreeView View { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Project Project { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public TreeView TreeView => View.GetVisualDescendants().OfType<TreeView>().First();

            public TreeViewItem RowOf(TreeNode node) =>
                View.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

            public Layer LayerOfA(int index) => (Layer)A.LayerManager.ManagerData.Items[index];
            public TreeNode LayerNodeOfA(int index) => Tree.Roots[0].Children[index];
            public TreeNode Group(int layerIndex, string header) => LayerNodeOfA(layerIndex).Children.Single(group => group.Header == header);

            // A right click on the row.
            public ContextMenu? OpenMenu(TreeNode node)
            {
                var row = RowOf(node);
                row.RaiseEvent(new ContextRequestedEventArgs { Source = row });
                Dispatcher.UIThread.RunJobs();
                return TreeView.ContextMenu;
            }

            public static List<string> Headers(ContextMenu menu) =>
                menu.Items.OfType<MenuItem>().Where(item => item.IsVisible).Select(item => item.Header?.ToString() ?? string.Empty).ToList();

            public static MenuItem Submenu(ContextMenu menu, string header) =>
                menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, header));

            public static void ClickSub(ContextMenu menu, string header, string subHeader)
            {
                Submenu(menu, header).Items.OfType<MenuItem>().Single(item => Equals(item.Header, subHeader))
                    .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Dispatcher.UIThread.RunJobs();
            }

            public static void Click(ContextMenu menu, string header)
            {
                menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, header))
                    .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Dispatcher.UIThread.RunJobs();
            }
        }

        // A holds two layers, B holds one.
        private Screen Show()
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
            var view = new ProjectTreeView { DataContext = tree };
            var window = new Window { Content = view, Width = 400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var screen = new Screen { Window = window, View = view, Tree = tree, Project = project, A = a, B = b };
            screens.Add(screen);
            return screen;
        }

        [AvaloniaFact]
        public void ACompositionMenu_StartsWithTheNameAndHasTheRowsOfTheLists()
        {
            var screen = Show();

            var headers = Screen.Headers(screen.OpenMenu(screen.Tree.Roots[0])!);

            Assert.Equal(screen.A.PrefabService.Name.Value, headers[0]);
            Assert.Equal(new[] { "Add Layer", "-", "Duplicate", "Rename", "-", "Move Up", "Move Down", "Move To Top", "Move To Bottom", "-", "Delete" },
                headers.Skip(2));
        }

        [AvaloniaFact]
        public void ARightClickOnARow_OpensTheMenu()
        {
            var screen = Show();

            var menu = screen.OpenMenu(screen.Tree.Roots[0])!;

            Assert.True(menu.IsOpen);
        }

        [AvaloniaFact]
        public void ASecondRightClick_OpensTheMenuOfTheNewRow()
        {
            var screen = Show();
            screen.OpenMenu(screen.Tree.Roots[0]);

            var menu = screen.OpenMenu(screen.Tree.Roots[1])!;

            Assert.True(menu.IsOpen);
            Assert.Equal(screen.B.PrefabService.Name.Value, Screen.Headers(menu)[0]);
        }

        // The menu of a row must be the menu of that row, whatever rows were clicked before and whatever rows are above it.
        [AvaloniaFact]
        public void ARightClickOnAGroupAfterARightClickOnItsComposition_OpensTheMenuOfTheGroup()
        {
            var screen = Show();
            screen.OpenMenu(screen.Tree.Roots[0]);
            screen.OpenMenu(screen.LayerNodeOfA(0));

            var entities = Screen.Headers(screen.OpenMenu(screen.Group(0, "Entities"))!);
            var cameras = Screen.Headers(screen.OpenMenu(screen.Group(0, "Cameras"))!);
            var lights = Screen.Headers(screen.OpenMenu(screen.Group(0, "Lights"))!);

            Assert.Equal(new[] { "Entities", "-", "Add Entity", "Add Fullscreen Quad", "Add Text" }, entities);
            Assert.Equal(new[] { "Cameras", "-", "Add Camera" }, cameras);
            Assert.Equal(new[] { "Lights", "-", "Add Light" }, lights);
        }

        [AvaloniaFact]
        public void ARightClickOnAnEntityAfterARightClickOnItsGroup_OpensTheMenuOfTheEntityWithNoAddRow()
        {
            var screen = Show();
            screen.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();
            var entityNode = screen.Group(0, "Entities").Children[0];
            screen.OpenMenu(screen.Tree.Roots[0]);
            screen.OpenMenu(screen.Group(0, "Entities"));

            var headers = Screen.Headers(screen.OpenMenu(entityNode)!);

            Assert.Equal(new[] { "Duplicate", "Rename", "-", "Move Up", "Move Down", "Move To Top", "Move To Bottom", "-", "Remove" },
                headers.Skip(2));
            Assert.DoesNotContain(headers, header => header.StartsWith("Add"));
        }

        [AvaloniaTheory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void EveryRightClick_OpensExactlyOneMenu_WhateverWasClickedBefore(int rounds)
        {
            var screen = Show();
            screen.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();
            var order = new[] { screen.Tree.Roots[0], screen.LayerNodeOfA(0), screen.Group(0, "Entities"), screen.Group(0, "Entities").Children[0] };

            for (var round = 0; round < rounds; round++)
                foreach (var node in order)
                {
                    var menu = screen.OpenMenu(node)!;

                    Assert.True(menu.IsOpen);
                    Assert.Equal(node.Header, Screen.Headers(menu)[0]);
                }

            Assert.All(screen.View.GetVisualDescendants().OfType<TreeViewItem>(), row => Assert.Null(row.ContextMenu));
        }

        [AvaloniaFact]
        public void AddLayer_AddsALayerToThatComposition()
        {
            var screen = Show();

            Screen.Click(screen.OpenMenu(screen.Tree.Roots[1])!, "Add Layer");

            Assert.Equal(2, screen.B.LayerManager.ManagerData.Items.Count);
            Assert.Equal(2, screen.Tree.Roots[1].Children.Count);
        }

        [AvaloniaFact]
        public void ALayerMenu_StartsWithTheAddSubmenuThenHasTheRowsOfTheLayerList()
        {
            var screen = Show();

            var headers = Screen.Headers(screen.OpenMenu(screen.LayerNodeOfA(0))!);

            Assert.Equal(new[] { "Add", "-", "Duplicate", "Rename", "-", "Move Up", "Move Down", "Move To Top", "Move To Bottom", "-", "Delete" },
                headers.Skip(2));
        }

        [AvaloniaFact]
        public void TheAddSubmenuOfALayer_OffersEntitiesCameraAndLight()
        {
            var screen = Show();

            var add = Screen.Submenu(screen.OpenMenu(screen.LayerNodeOfA(0))!, "Add");

            Assert.Equal(new[] { "Entity", "Fullscreen Quad", "Text", "Camera", "Light" },
                add.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()));
        }

        [AvaloniaFact]
        public void TheAddSubmenuOfALayer_AddsEachKindToTheRightListOfThatLayer()
        {
            var screen = Show();
            var layer = screen.LayerOfA(1);
            var node = screen.LayerNodeOfA(1);

            foreach (var header in new[] { "Entity", "Fullscreen Quad", "Text", "Camera", "Light" })
                Screen.ClickSub(screen.OpenMenu(node)!, "Add", header);

            Assert.Collection(layer.ModelEntityManager.ManagerData.Items,
                item => Assert.IsType<Entity>(item),
                item => Assert.IsType<FullscreenQuad>(item),
                item => Assert.IsType<TextEntity>(item));
            Assert.IsType<Camera>(Assert.Single(layer.CameraManager.ManagerData.Items));
            Assert.IsType<LightEntity>(Assert.Single(layer.LightManager.ManagerData.Items));
            Assert.Empty(screen.LayerOfA(0).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void Duplicate_OnALayer_CopiesTheLayerRightAfterIt()
        {
            var screen = Show();
            var original = screen.LayerOfA(0);

            Screen.Click(screen.OpenMenu(screen.LayerNodeOfA(0))!, "Duplicate");

            Assert.Equal(3, screen.A.LayerManager.ManagerData.Items.Count);
            Assert.Same(original, screen.A.LayerManager.ManagerData.Items[0]);
            Assert.NotSame(original, screen.A.LayerManager.ManagerData.Items[1]);
        }

        [AvaloniaFact]
        public void MoveDown_OnTheFirstComposition_MovesItAndTheTree()
        {
            var screen = Show();

            Screen.Click(screen.OpenMenu(screen.Tree.Roots[0])!, "Move Down");

            Assert.Equal(new IControl[] { screen.B, screen.A }, screen.Project.CompositionManager.ManagerData.Items);
            Assert.Same(screen.B, screen.Tree.Roots[0].Item);
        }

        [AvaloniaFact]
        public void MoveUp_OnTheFirstRow_IsDisabled()
        {
            var screen = Show();

            var menu = screen.OpenMenu(screen.Tree.Roots[0])!;

            Assert.False(menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Move Up")).IsEnabled);
            Assert.True(menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, "Move Down")).IsEnabled);
        }

        [AvaloniaFact]
        public void TheMenu_ActsOnTheRowThatWasClicked_NotOnTheSelectedRow()
        {
            var screen = Show();
            var first = screen.LayerOfA(0);
            var second = screen.LayerOfA(1);
            screen.Tree.SelectedNode = screen.LayerNodeOfA(1);

            Screen.Click(screen.OpenMenu(screen.LayerNodeOfA(0))!, "Delete");

            Assert.Equal(new IControl[] { second }, screen.A.LayerManager.ManagerData.Items);
            Assert.DoesNotContain(first, screen.A.LayerManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void TheEntitiesGroupMenu_OnlyAddsEntities()
        {
            var screen = Show();

            var headers = Screen.Headers(screen.OpenMenu(screen.Group(0, "Entities"))!);

            Assert.Equal(new[] { "Entities", "-", "Add Entity", "Add Fullscreen Quad", "Add Text" }, headers);
        }

        [AvaloniaFact]
        public void TheAddRowsOfTheEntitiesGroup_AddTheirTypeToTheLayer()
        {
            var screen = Show();
            var layer = screen.LayerOfA(0);

            Screen.Click(screen.OpenMenu(screen.Group(0, "Entities"))!, "Add Entity");
            Screen.Click(screen.OpenMenu(screen.Group(0, "Entities"))!, "Add Fullscreen Quad");
            Screen.Click(screen.OpenMenu(screen.Group(0, "Entities"))!, "Add Text");

            Assert.Collection(layer.ModelEntityManager.ManagerData.Items,
                item => Assert.IsType<Entity>(item),
                item => Assert.IsType<FullscreenQuad>(item),
                item => Assert.IsType<TextEntity>(item));
            Assert.Equal(3, screen.Group(0, "Entities").Children.Count);
        }

        [AvaloniaFact]
        public void TheCamerasAndLightsGroups_AddACameraAndALight()
        {
            var screen = Show();
            var layer = screen.LayerOfA(0);

            Screen.Click(screen.OpenMenu(screen.Group(0, "Cameras"))!, "Add Camera");
            Screen.Click(screen.OpenMenu(screen.Group(0, "Lights"))!, "Add Light");

            Assert.IsType<Camera>(Assert.Single(layer.CameraManager.ManagerData.Items));
            Assert.IsType<LightEntity>(Assert.Single(layer.LightManager.ManagerData.Items));
        }

        [AvaloniaFact]
        public void AnEntityMenu_HasTheRowsOfTheSlots_WithRemove()
        {
            var screen = Show();
            screen.LayerOfA(0).ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();

            var headers = Screen.Headers(screen.OpenMenu(screen.Group(0, "Entities").Children[0])!);

            Assert.Equal(new[] { "Duplicate", "Rename", "-", "Move Up", "Move Down", "Move To Top", "Move To Bottom", "-", "Remove" },
                headers.Skip(2));
        }

        [AvaloniaFact]
        public void Remove_OnAnEntity_TakesItOutOfThatLayerOnly()
        {
            var screen = Show();
            var layer = screen.LayerOfA(0);
            layer.ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();

            Screen.Click(screen.OpenMenu(screen.Group(0, "Entities").Children[0])!, "Remove");

            Assert.Empty(layer.ModelEntityManager.ManagerData.Items);
            Assert.Empty(screen.Group(0, "Entities").Children);
        }

        [AvaloniaFact]
        public void ARightClickOutsideTheRows_OpensNoMenu()
        {
            var screen = Show();

            screen.TreeView.RaiseEvent(new ContextRequestedEventArgs { Source = screen.TreeView });
            Dispatcher.UIThread.RunJobs();

            Assert.Null(screen.TreeView.ContextMenu);
            Assert.All(screen.View.GetVisualDescendants().OfType<TreeViewItem>(), row => Assert.Null(row.ContextMenu));
        }

        private static bool IsRenaming(TextBoxEditable box) =>
            box.GetVisualDescendants().OfType<TextBox>().Any(input => input.IsVisible);

        // A double click on the name starts the rename.
        private static (TextBoxEditable Box, Point Point) StartRename(Screen screen, TreeNode node)
        {
            var box = screen.RowOf(node).GetVisualDescendants().OfType<TextBoxEditable>().First();
            var point = box.TranslatePoint(new Point(10, box.Bounds.Height / 2), screen.Window)!.Value;
            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            return (box, point);
        }

        [AvaloniaFact]
        public void AContextRequestWhileTheNameIsEdited_OpensNoMenu()
        {
            var screen = Show();
            var node = screen.LayerNodeOfA(0);
            var (box, _) = StartRename(screen, node);
            Assert.True(IsRenaming(box));

            var row = screen.RowOf(node);
            var input = box.GetVisualDescendants().OfType<TextBox>().First(textBox => textBox.IsVisible);
            input.RaiseEvent(new ContextRequestedEventArgs { Source = input });
            Dispatcher.UIThread.RunJobs();

            Assert.Null(screen.TreeView.ContextMenu);
        }

        [AvaloniaFact]
        public void ARightClickDuringARename_EndsTheRenameAndOpensNoMenu_AndTheNextOneOpensTheMenu()
        {
            var screen = Show();
            var node = screen.LayerNodeOfA(0);
            var row = screen.RowOf(node);
            var (box, point) = StartRename(screen, node);
            Assert.True(IsRenaming(box));

            screen.Window.MouseDown(point, MouseButton.Right);
            screen.Window.MouseUp(point, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();

            Assert.False(IsRenaming(box));
            Assert.Null(screen.TreeView.ContextMenu);

            screen.Window.MouseDown(point, MouseButton.Right);
            screen.Window.MouseUp(point, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();

            Assert.True(screen.TreeView.ContextMenu?.IsOpen);
        }

        [AvaloniaFact]
        public void Rename_StartsTheEditOfTheNameOfThatRow()
        {
            var screen = Show();
            var node = screen.LayerNodeOfA(1);

            Screen.Click(screen.OpenMenu(node)!, "Rename");

            var box = screen.RowOf(node).GetVisualDescendants().OfType<CMiX.UI.Controls.String.TextBoxEditable>().First();
            Assert.True(box.IsEditing);
        }
    }
}
