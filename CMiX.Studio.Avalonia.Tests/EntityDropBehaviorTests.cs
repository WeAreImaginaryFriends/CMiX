using Avalonia.Headless.XUnit;
using CMiX.Core;
using CMiX.Core.Assets;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Texturing.Sources;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Behaviors;
using CMiX.UI.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Image = CMiX.Core.Texturing.Sources.Image;

namespace CMiX.Studio.Avalonia.Tests
{
    // Files and folders dropped on an entity list become entities that point to them.
    public class EntityDropBehaviorTests : IDisposable
    {
        private readonly string _folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cmix-entity-drop-" + Guid.NewGuid().ToString("N"));

        public EntityDropBehaviorTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose() => Directory.Delete(_folder, true);

        private sealed class Dropper : EntityDropBehavior
        {
            public void Drop(IReadOnlyList<string> paths) => OnPathsDropped(paths);
        }

        private string NewFile(string name)
        {
            var path = System.IO.Path.Combine(_folder, name);
            File.WriteAllText(path, "x");
            return path;
        }

        private string NewFolder(string name, params string[] files)
        {
            var folder = System.IO.Path.Combine(_folder, name);
            Directory.CreateDirectory(folder);
            foreach (var file in files)
                File.WriteAllText(System.IO.Path.Combine(folder, file), "x");
            return folder;
        }

        private static (PrefabManager Entities, UndoManager Undo, Dropper Dropper) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var entities = provider.GetRequiredService<PrefabManager>();
            var undo = provider.GetRequiredService<UndoManager>();
            return (entities, undo, new Dropper { PrefabManager = entities });
        }

        private static IAssetTextureSource TextureOf(FullscreenQuad quad) =>
            Assert.IsAssignableFrom<IAssetTextureSource>(
                Assert.Single(quad.Texture.DiffuseTexture.TextureManager.ManagerData.Items));

        [AvaloniaFact]
        public void TheBehavior_AcceptsFilesAndFoldersByDefault()
        {
            Assert.Equal(FileDropMode.FilesAndFolders, new EntityDropBehavior().Mode);
        }

        [AvaloniaFact]
        public void ADroppedFolderWithImages_BecomesAQuadWithASequencePlayer()
        {
            var (entities, _, dropper) = Show();
            var folder = NewFolder("frames", "frame_0001.png");

            dropper.Drop(new[] { folder });

            var quad = Assert.IsType<FullscreenQuad>(Assert.Single(entities.ManagerData.Items));
            var source = TextureOf(quad);
            Assert.IsType<SequencePlayer>(source);
            Assert.Equal(folder, source.AssetSelector.Asset.FilePath);
        }

        [AvaloniaFact]
        public void ADroppedFolderWithoutImages_AddsNothing()
        {
            var (entities, _, dropper) = Show();

            dropper.Drop(new[] { NewFolder("docs", "notes.txt") });

            Assert.Empty(entities.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADroppedImage_BecomesAQuadWithAnImage()
        {
            var (entities, _, dropper) = Show();
            var path = NewFile("one.png");

            dropper.Drop(new[] { path });

            var quad = Assert.IsType<FullscreenQuad>(Assert.Single(entities.ManagerData.Items));
            var source = TextureOf(quad);
            Assert.IsType<Image>(source);
            Assert.Equal(path, source.AssetSelector.Asset.FilePath);
        }

        [AvaloniaFact]
        public void ADroppedVideo_BecomesAQuadWithAVideoPlayer()
        {
            var (entities, _, dropper) = Show();
            var path = NewFile("clip.mp4");

            dropper.Drop(new[] { path });

            var quad = Assert.IsType<FullscreenQuad>(Assert.Single(entities.ManagerData.Items));
            var source = TextureOf(quad);
            Assert.IsType<VideoPlayer>(source);
            Assert.Equal(path, source.AssetSelector.Asset.FilePath);
        }

        [AvaloniaFact]
        public void ADroppedGeometryFile_BecomesAnEntityWithThatMesh()
        {
            var (entities, _, dropper) = Show();
            var path = NewFile("model.obj");

            dropper.Drop(new[] { path });

            var entity = Assert.IsType<Entity>(Assert.Single(entities.ManagerData.Items));
            Assert.Equal(MeshType.FromFile, entity.Mesh.MeshTypeSelector.Value);
            Assert.Equal(path, entity.Mesh.Geometry.Asset.FilePath);
            Assert.IsType<Geometry>(entity.Mesh.Geometry.Asset);
        }

        [AvaloniaFact]
        public void ADroppedFileOfAnUnknownType_AddsNothing()
        {
            var (entities, _, dropper) = Show();

            dropper.Drop(new[] { NewFile("notes.txt") });

            Assert.Empty(entities.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADroppedQuad_IsOneUndoStep()
        {
            var (entities, undo, dropper) = Show();
            undo.Clear();

            dropper.Drop(new[] { NewFile("one.png") });
            undo.Undo();

            Assert.Empty(entities.ManagerData.Items);
        }

        [AvaloniaFact]
        public void ADroppedGeometryFile_IsOneUndoStep()
        {
            var (entities, undo, dropper) = Show();
            undo.Clear();

            dropper.Drop(new[] { NewFile("model.obj") });
            undo.Undo();

            Assert.Empty(entities.ManagerData.Items);
        }
    }
}
