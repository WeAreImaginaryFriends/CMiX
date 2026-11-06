using Avalonia.Headless.XUnit;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Texturing.Sources;
using CMiX.Studio.Avalonia.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Image = CMiX.Core.Texturing.Sources.Image;

namespace CMiX.Studio.Avalonia.Tests
{
    // The rules of a dropped path for a texture list.
    public class PathDropServiceTests : IDisposable
    {
        private readonly string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cmix-path-drop-" + Guid.NewGuid().ToString("N"));
        private readonly PathDropService service = new();

        public PathDropServiceTests()
        {
            Directory.CreateDirectory(folder);
        }

        public void Dispose() => Directory.Delete(folder, true);

        private string NewFile(string name)
        {
            var path = System.IO.Path.Combine(folder, name);
            File.WriteAllText(path, "x");
            return path;
        }

        [AvaloniaFact]
        public void TextureTypeOf_AFolderWithImages_IsSequencePlayer()
        {
            var frames = System.IO.Path.Combine(folder, "frames");
            Directory.CreateDirectory(frames);
            File.WriteAllText(System.IO.Path.Combine(frames, "frame_0001.png"), "x");

            Assert.Equal(typeof(SequencePlayer), service.TextureTypeOf(frames));
        }

        [AvaloniaFact]
        public void TextureTypeOf_AVideo_IsVideoPlayer()
        {
            Assert.Equal(typeof(VideoPlayer), service.TextureTypeOf(NewFile("clip.mp4")));
        }

        [AvaloniaFact]
        public void TextureTypeOf_AnImage_IsImage()
        {
            Assert.Equal(typeof(Image), service.TextureTypeOf(NewFile("one.png")));
        }

        [AvaloniaFact]
        public void TextureTypeOf_AnUnknownFile_IsNull()
        {
            Assert.Null(service.TextureTypeOf(NewFile("notes.xyz")));
            Assert.False(service.AcceptsTexture(NewFile("notes.txt")));
        }

        [AvaloniaFact]
        public void AddTexture_AnImage_AddsAnImageWithThePath()
        {
            var provider = TestServiceProviderFactory.Create();
            var textures = provider.GetRequiredService<PrefabManager>();
            var path = NewFile("one.png");

            provider.GetRequiredService<PathDropService>().AddTexture(textures, path);

            var image = Assert.IsType<Image>(Assert.Single(textures.ManagerData.Items));
            Assert.Equal(path, image.AssetSelector.Asset.FilePath);
        }
    }
}
