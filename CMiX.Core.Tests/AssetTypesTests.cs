using CMiX.Core.Assets;
using Xunit;

namespace CMiX.Core.Tests
{
    public class AssetTypesTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cmix-assettypes-" + Guid.NewGuid().ToString("N"));

        public AssetTypesTests() => Directory.CreateDirectory(_folder);

        public void Dispose() => Directory.Delete(_folder, true);

        [Fact]
        public void ContainsImages_AFolderWithAnImage_IsTrue()
        {
            File.WriteAllText(Path.Combine(_folder, "frame_0001.png"), "x");

            Assert.True(AssetTypes.ContainsImages(_folder));
        }

        [Fact]
        public void ContainsImages_AFolderWithoutImages_IsFalse()
        {
            File.WriteAllText(Path.Combine(_folder, "notes.txt"), "x");
            File.WriteAllText(Path.Combine(_folder, "clip.mp4"), "x");

            Assert.False(AssetTypes.ContainsImages(_folder));
        }

        [Fact]
        public void ContainsImages_AnImageInASubFolder_IsFalse()
        {
            var sub = Directory.CreateDirectory(Path.Combine(_folder, "sub")).FullName;
            File.WriteAllText(Path.Combine(sub, "frame.png"), "x");

            Assert.False(AssetTypes.ContainsImages(_folder));
        }

        [Fact]
        public void ContainsImages_AMissingFolder_IsFalse()
        {
            Assert.False(AssetTypes.ContainsImages(Path.Combine(_folder, "missing")));
        }
    }
}
