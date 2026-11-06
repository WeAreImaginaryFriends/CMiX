using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using FullscreenQuad = CMiX.Core.Compositing.FullscreenQuad;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;
using TextEntity = CMiX.Core.Compositing.TextEntity;

namespace CMiX.Studio.Avalonia.Tests
{
    // Files and folders dropped on the tree make the compositions, layers and entities that they need.
    public class ProjectTreePathDropTests : IDisposable
    {
        private readonly string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cmix-tree-drop-" + Guid.NewGuid().ToString("N"));
        private readonly List<Window> windows = new();

        public ProjectTreePathDropTests()
        {
            Directory.CreateDirectory(folder);
        }

        public void Dispose()
        {
            foreach (var window in windows)
                window.Close();
            Directory.Delete(folder, true);
        }

        private string NewFile(string name, string content = "x")
        {
            var path = System.IO.Path.Combine(folder, name);
            File.WriteAllText(path, content);
            return path;
        }

        private string NewFrames()
        {
            var frames = System.IO.Path.Combine(folder, "frames");
            Directory.CreateDirectory(frames);
            File.WriteAllText(System.IO.Path.Combine(frames, "frame_0001.png"), "x");
            return frames;
        }

        private sealed class Screen
        {
            public Window Window { get; init; } = null!;
            public ProjectTreeView View { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Project Project { get; init; } = null!;
            public UndoManager Undo { get; init; } = null!;
        }

        private Screen Show(int compositions = 0, int layersEach = 0)
        {
            var scene = CreateScene(Enumerable.Repeat(layersEach, compositions).ToArray());
            var screen = TreeTestScreen.Show(scene.Tree, 400, 600, instant: false, settle: false);
            windows.Add(screen.Window);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            scene.Undo.Clear();
            return new Screen { Window = screen.Window, View = screen.View, Tree = scene.Tree, Project = scene.Project, Undo = scene.Undo };
        }

        private static Composition CompositionAt(Screen screen, int index) =>
            (Composition)screen.Project.CompositionManager.ManagerData.Items[index];

        private static Layer LayerAt(Composition composition, int index) =>
            (Layer)composition.LayerManager.ManagerData.Items[index];

        // ---- the tree model ----

        [AvaloniaFact]
        public void AnImageDroppedOnAnEmptyTree_MakesACompositionWithALayerAndAnEntity()
        {
            var screen = Show();

            screen.Tree.DropPaths(null, new[] { NewFile("one.png") });

            var composition = CompositionAt(screen, 0);
            var layer = LayerAt(composition, 0);
            Assert.IsType<Entity>(Assert.Single(layer.ModelEntityManager.ManagerData.Items));
            Assert.Single(screen.Project.CompositionManager.ManagerData.Items);
            Assert.Single(composition.LayerManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void AFolderWithImagesDroppedOnAnEmptyTree_MakesAQuad()
        {
            var screen = Show();

            screen.Tree.DropPaths(null, new[] { NewFrames() });

            var layer = LayerAt(CompositionAt(screen, 0), 0);
            Assert.IsType<FullscreenQuad>(Assert.Single(layer.ModelEntityManager.ManagerData.Items));
        }

        [AvaloniaFact]
        public void ATextFileDroppedOnAnEmptyTree_MakesATextEntity()
        {
            var screen = Show();

            screen.Tree.DropPaths(null, new[] { NewFile("notes.txt", "Hello") });

            var layer = LayerAt(CompositionAt(screen, 0), 0);
            var text = Assert.IsType<TextEntity>(Assert.Single(layer.ModelEntityManager.ManagerData.Items));
            Assert.Equal("Hello", text.Text.Value);
        }

        [AvaloniaFact]
        public void ADropOnTheSpaceOfATreeWithCompositions_MakesANewComposition()
        {
            var screen = Show(compositions: 1, layersEach: 1);

            screen.Tree.DropPaths(null, new[] { NewFile("one.png") });

            Assert.Equal(2, screen.Project.CompositionManager.ManagerData.Items.Count);
            Assert.Empty(LayerAt(CompositionAt(screen, 0), 0).ModelEntityManager.ManagerData.Items);
            Assert.Single(LayerAt(CompositionAt(screen, 1), 0).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADropOnACompositionWithoutLayers_MakesALayerWithTheEntity()
        {
            var screen = Show(compositions: 1);

            screen.Tree.DropPaths(screen.Tree.Roots[0], new[] { NewFile("one.png") });

            var composition = CompositionAt(screen, 0);
            Assert.Single(screen.Project.CompositionManager.ManagerData.Items);
            Assert.Single(LayerAt(composition, 0).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADropOnACompositionWithLayers_AlwaysMakesANewLayer()
        {
            var screen = Show(compositions: 1, layersEach: 2);

            screen.Tree.DropPaths(screen.Tree.Roots[0], new[] { NewFile("one.png") });

            var composition = CompositionAt(screen, 0);
            Assert.Equal(3, composition.LayerManager.ManagerData.Items.Count);
            Assert.Empty(LayerAt(composition, 0).ModelEntityManager.ManagerData.Items);
            Assert.Empty(LayerAt(composition, 1).ModelEntityManager.ManagerData.Items);
            Assert.Single(LayerAt(composition, 2).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADropOnALayer_AddsTheEntityToThatLayer()
        {
            var screen = Show(compositions: 1, layersEach: 2);
            var secondLayer = screen.Tree.Roots[0].Children[1];

            screen.Tree.DropPaths(secondLayer, new[] { NewFile("one.png") });

            var composition = CompositionAt(screen, 0);
            Assert.Equal(2, composition.LayerManager.ManagerData.Items.Count);
            Assert.Empty(LayerAt(composition, 0).ModelEntityManager.ManagerData.Items);
            Assert.Single(LayerAt(composition, 1).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADropOnTheGroupsOrItemsOfALayer_AddsTheEntityToTheEntitiesOfThatLayer()
        {
            var screen = Show(compositions: 1, layersEach: 1);
            var layerNode = screen.Tree.Roots[0].Children[0];
            var entities = layerNode.Children[0];
            var cameras = layerNode.Children[1];
            var layer = LayerAt(CompositionAt(screen, 0), 0);

            screen.Tree.DropPaths(entities, new[] { NewFile("one.png") });
            screen.Tree.DropPaths(cameras, new[] { NewFile("two.png") });
            screen.Tree.DropPaths(entities.Children[0], new[] { NewFile("three.png") });

            Assert.Equal(3, layer.ModelEntityManager.ManagerData.Items.Count);
            Assert.Empty(layer.CameraManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void SeveralPaths_GoToOneNewLayer()
        {
            var screen = Show(compositions: 1);

            screen.Tree.DropPaths(screen.Tree.Roots[0], new[] { NewFile("one.png"), NewFrames(), NewFile("notes.txt") });

            var composition = CompositionAt(screen, 0);
            Assert.Single(composition.LayerManager.ManagerData.Items);
            Assert.Equal(3, LayerAt(composition, 0).ModelEntityManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void ADropOfAnUnknownFile_ChangesNothing()
        {
            var screen = Show();

            screen.Tree.DropPaths(null, new[] { NewFile("notes.xyz") });

            Assert.Empty(screen.Project.CompositionManager.ManagerData.Items);
            Assert.False(screen.Tree.CanDropPaths(new[] { NewFile("other.xyz") }));
        }

        [AvaloniaFact]
        public void ADropThatMakesTheWholeChain_IsOneUndoStep()
        {
            var screen = Show();

            screen.Tree.DropPaths(null, new[] { NewFile("one.png"), NewFile("two.png") });
            screen.Undo.Undo();

            Assert.Empty(screen.Project.CompositionManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADropOnALayer_IsOneUndoStep()
        {
            var screen = Show(compositions: 1, layersEach: 1);

            screen.Tree.DropPaths(screen.Tree.Roots[0].Children[0], new[] { NewFile("one.png"), NewFile("two.png") });
            screen.Undo.Undo();

            Assert.Empty(LayerAt(CompositionAt(screen, 0), 0).ModelEntityManager.ManagerData.Items);
        }

        // ---- the view ----

        private static DataObject FilesOf(TopLevel window, params string[] paths)
        {
            var provider = window.StorageProvider;
            var items = new List<IStorageItem>();
            foreach (var path in paths)
            {
                var uri = new Uri(path);
                IStorageItem? item = Directory.Exists(path)
                    ? provider.TryGetFolderFromPathAsync(uri).GetAwaiter().GetResult()
                    : provider.TryGetFileFromPathAsync(uri).GetAwaiter().GetResult();
                items.Add(item!);
            }

            var data = new DataObject();
            data.Set(DataFormats.Files, items.ToArray());
            return data;
        }

        private static TreeViewItem ItemOf(Screen screen, TreeNode node) => TreeTestScreen.ItemOf(screen.View, node);

        // The template child, not a border of the rows under the node.
        private static Border RowOf(Screen screen, TreeNode node) =>
            ItemOf(screen, node).GetTemplateChildren().OfType<Border>().First(border => border.Name == "RowBorder");

        private static void Drag(Screen screen, Point point, DataObject data, RawDragEventType last)
        {
            var effects = DragDropEffects.Copy | DragDropEffects.Move;
            screen.Window.DragDrop(point, RawDragEventType.DragEnter, data, effects);
            screen.Window.DragDrop(point, RawDragEventType.DragOver, data, effects);
            if (last == RawDragEventType.Drop)
                screen.Window.DragDrop(point, RawDragEventType.Drop, data, effects);
            Dispatcher.UIThread.RunJobs();
        }

        private static Point RowPoint(Screen screen, TreeNode node)
        {
            var row = ItemOf(screen, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            return row.TranslatePoint(new Point(120, 12), screen.Window)!.Value;
        }

        // Under the rows and above the toolbar.
        private static Point EmptySpace(Screen screen) => new(200, 300);

        [AvaloniaFact]
        public void FilesDroppedOnTheEmptySpaceOfTheView_MakeTheChain()
        {
            var screen = Show();

            Drag(screen, EmptySpace(screen), FilesOf(screen.Window, NewFile("one.png")), RawDragEventType.Drop);

            Assert.Single(LayerAt(CompositionAt(screen, 0), 0).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void FilesDroppedOnARowOfTheView_MakeALayerUnderTheComposition()
        {
            var screen = Show(compositions: 1, layersEach: 1);

            Drag(screen, RowPoint(screen, screen.Tree.Roots[0]), FilesOf(screen.Window, NewFile("one.png")), RawDragEventType.Drop);

            var composition = CompositionAt(screen, 0);
            Assert.Equal(2, composition.LayerManager.ManagerData.Items.Count);
            Assert.Single(LayerAt(composition, 1).ModelEntityManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void OverARow_OnlyTheRowThatTakesTheFilesLooksHovered_AndNoFrameIsDrawn()
        {
            var screen = Show(compositions: 1, layersEach: 1);
            var data = FilesOf(screen.Window, NewFile("one.png"));
            var layer = screen.Tree.Roots[0].Children[0];
            var composition = screen.Tree.Roots[0];
            var entities = layer.Children[0];
            var cameras = layer.Children[1];
            var palette = CategoryPalette.FromApplication();
            bool Hovered(TreeNode node) =>
                (RowOf(screen, node).Background as ISolidColorBrush)?.Color == palette.For(node.Category!.Value, node.ComposableDepth, node.KindDepth).FillSelected;

            // Over the layer: only the layer looks hovered, not its children.
            Drag(screen, RowPoint(screen, layer), data, RawDragEventType.DragOver);
            Assert.True(Hovered(layer));
            Assert.False(Hovered(composition) || Hovered(entities) || Hovered(cameras));

            // Over the group of entities, which takes the files itself: that group looks hovered, not the layer.
            Drag(screen, RowPoint(screen, entities), data, RawDragEventType.DragOver);
            Assert.True(Hovered(entities));
            Assert.False(Hovered(layer) || Hovered(composition) || Hovered(cameras));

            // Over a child that passes the drop to the entities of the layer: nothing looks hovered.
            Drag(screen, RowPoint(screen, cameras), data, RawDragEventType.DragOver);
            Assert.False(Hovered(layer) || Hovered(composition) || Hovered(entities) || Hovered(cameras));

            // Over the composition: the composition looks hovered.
            Drag(screen, RowPoint(screen, composition), data, RawDragEventType.DragOver);
            Assert.True(Hovered(composition));
            Assert.False(Hovered(layer));

            // Over the empty space nothing looks hovered: the drop makes a new composition.
            Drag(screen, EmptySpace(screen), data, RawDragEventType.DragOver);
            Assert.False(Hovered(composition) || Hovered(layer));
            Assert.Empty(screen.View.GetVisualDescendants().OfType<Border>().Where(border => border.Name == "DropFrame"));
        }

        [AvaloniaFact]
        public void DropPaths_ThreeFiles_IsOneUndoStep()
        {
            var screen = Show(compositions: 1, layersEach: 1);
            var layer = LayerAt(CompositionAt(screen, 0), 0);

            screen.Tree.DropPaths(screen.Tree.Roots[0].Children[0], new[] { NewFile("one.png"), NewFrames(), NewFile("notes.txt") });
            Assert.Equal(3, layer.ModelEntityManager.ManagerData.Items.Count);
            screen.Undo.Undo();

            Assert.Empty(layer.ModelEntityManager.ManagerData.Items);
        }

        // The view classifies the paths on DragEnter. A DragOver after that reads the record, not the folder:
        // the folder is gone, but the row still takes the drag.
        [AvaloniaFact]
        public void DragOver_AfterDragEnter_ReadsNoFolder()
        {
            var screen = Show(compositions: 1);
            var frames = NewFrames();
            var data = FilesOf(screen.Window, frames);
            var composition = screen.Tree.Roots[0];
            var point = RowPoint(screen, composition);
            var palette = CategoryPalette.FromApplication();
            var effects = DragDropEffects.Copy | DragDropEffects.Move;

            screen.Window.DragDrop(point, RawDragEventType.DragEnter, data, effects);
            Directory.Delete(frames, true);
            screen.Window.DragDrop(point, RawDragEventType.DragOver, data, effects);
            Dispatcher.UIThread.RunJobs();

            var row = RowOf(screen, composition);
            Assert.Equal(
                palette.For(composition.Category!.Value, composition.ComposableDepth, composition.KindDepth).FillSelected,
                (row.Background as ISolidColorBrush)?.Color);
        }

        [AvaloniaFact]
        public void FilesThatTheTreeDoesNotAccept_MakeNothing()
        {
            var screen = Show();
            var data = FilesOf(screen.Window, NewFile("notes.xyz"));

            Drag(screen, EmptySpace(screen), data, RawDragEventType.Drop);

            Assert.Empty(screen.Project.CompositionManager.ManagerData.Items);
        }
    }
}
