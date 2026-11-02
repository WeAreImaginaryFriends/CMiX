using System.Text.Json;
using System.Text.Json.Nodes;
using CMiX.Core.Assets;
using CMiX.Core.Persistence;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Sources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class SequencePlayerTests
    {
        private static ControlFactory CreateFactory() =>
            TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>();

        [Fact]
        public void NewSequencePlayer_HasTheDefaultValues()
        {
            var player = (SequencePlayer)CreateFactory().Create(typeof(SequencePlayer));

            Assert.Equal(60f, player.FPS.Value);
            Assert.True(player.Looping.Value);
            Assert.True(player.Playing.Value);
            Assert.False(player.DoSeek.Value);
            Assert.IsAssignableFrom<IAssetTextureSource>(player);
        }

        [Fact]
        public void SequencePlayer_KeepsEveryValueThroughSaveAndLoad()
        {
            using var temp = new TempDirectoryFixture();
            var factory = CreateFactory();

            var player = (SequencePlayer)factory.Create(typeof(SequencePlayer));
            player.FPS.Value = 24f;
            player.Looping.Value = false;
            player.SeekTarget.Value = 7;
            player.Playing.Value = false;
            player.Resolution.X.Value = 640;
            player.Resolution.Y.Value = 360;
            player.AssetSelector.SetAssetFromPath(temp.Path);
            var model = player.ToModel();

            var loaded = (SequencePlayer)factory.Create(typeof(SequencePlayer));
            loaded.FromModel(model);

            Assert.Equal(24f, loaded.FPS.Value);
            Assert.False(loaded.Looping.Value);
            Assert.Equal(7, loaded.SeekTarget.Value);
            Assert.False(loaded.Playing.Value);
            Assert.Equal(640, loaded.Resolution.X.Value);
            Assert.Equal(360, loaded.Resolution.Y.Value);
            var sequence = Assert.IsType<ImageSequence>(loaded.AssetSelector.Asset);
            Assert.Equal(temp.Path, sequence.FilePath);
        }

        [Fact]
        public void FromModel_OfAnOldFileWithAButtonDoSeek_LoadsAFalseValueWithTheID()
        {
            var id = new Guid("5a1d3c2e-7b4f-4e8a-9c61-2f0d8e4b7a93");
            var node = JsonSerializer.SerializeToNode(new SequencePlayerModel(), ProjectSerializer.Options)!;
            node["DoSeek"] = new JsonObject { ["ID"] = id.ToString() };

            var loaded = JsonSerializer.Deserialize<SequencePlayerModel>(node, ProjectSerializer.Options)!;

            Assert.Equal(id, loaded.DoSeek.ID);
            Assert.False(loaded.DoSeek.Value.Value);
            Assert.Null(loaded.DoSeek.ModulatorID.Value);
        }
    }
}
