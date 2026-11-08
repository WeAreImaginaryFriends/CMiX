using System.Text.Json;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Persistence;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Sources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class LiquishTests
    {
        private static Liquish Create() =>
            (Liquish)TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>().Create(typeof(Liquish));

        private static LFOModulator AddModulator(Liquish liquish)
        {
            liquish.ModulatorManager.AddItem(typeof(LFOModulator));
            return (LFOModulator)liquish.ModulatorManager.ManagerData.Items[0];
        }

        [Fact]
        public void Create_NewLiquish_HasTheDefaultValues()
        {
            var liquish = Create();

            Assert.Equal(512, liquish.Resolution.X.Value);
            Assert.Equal(512, liquish.Resolution.Y.Value);
            Assert.False(liquish.UseCompositionResolution.Value);
            Assert.Equal("Time", liquish.Time.Label);
            Assert.Equal(0.0f, liquish.Time.Value);
            Assert.Equal(5f, liquish.Scale.X.Value);
            Assert.Equal(5f, liquish.Scale.Y.Value);
            Assert.Equal("#FF000000", liquish.ColorA.Value);
            Assert.Equal("#FFFFFFFF", liquish.ColorB.Value);
            Assert.Equal("#FF800026", liquish.ColorC.Value);
        }

        [Fact]
        public void FromModel_OfToModel_KeepsEveryValue()
        {
            var liquish = Create();
            liquish.Time.Value = 2.5f;
            liquish.Scale.X.Value = 3f;
            liquish.Scale.Y.Value = 7f;
            liquish.ColorA.Value = "#FF112233";
            liquish.ColorB.Value = "#FF445566";
            liquish.ColorC.Value = "#FF778899";

            var loaded = Create();
            loaded.FromModel(liquish.ToModel());

            Assert.Equal(2.5f, loaded.Time.Value);
            Assert.Equal(3f, loaded.Scale.X.Value);
            Assert.Equal(7f, loaded.Scale.Y.Value);
            Assert.Equal("#FF112233", loaded.ColorA.Value);
            Assert.Equal("#FF445566", loaded.ColorB.Value);
            Assert.Equal("#FF778899", loaded.ColorC.Value);
            Assert.Equal(512, loaded.Resolution.X.Value);
            Assert.Equal(512, loaded.Resolution.Y.Value);
            Assert.Equal(liquish.ID, loaded.ID);
        }

        [Fact]
        public void ModulatorManager_RemoveItem_UnbindsTheTimeValue()
        {
            var liquish = Create();
            var modulator = AddModulator(liquish);
            liquish.Time.SetModulatorCommand.Execute(new ModulatorOutputSelection(modulator, modulator.Outputs[0]));
            Assert.Equal(modulator.ID, liquish.Time.ModulatorID);

            liquish.ModulatorManager.RemoveItem(modulator);

            Assert.Null(liquish.Time.ModulatorID);
        }

        [Fact]
        public void Scale_BoundToAModulator_FollowsItThroughTheModel()
        {
            var liquish = Create();
            var modulator = AddModulator(liquish);
            liquish.Scale.X.SetModulatorCommand.Execute(new ModulatorOutputSelection(modulator, modulator.Outputs[0]));

            liquish.FromModel(liquish.ToModel());

            Assert.Equal(modulator.ID, liquish.Scale.X.ModulatorID);
        }

        [Fact]
        public void Serialize_ThenDeserialize_KeepsTheModel()
        {
            var liquish = Create();
            liquish.Scale.Y.Value = 9f;
            liquish.ColorC.Value = "#FF010203";
            var model = liquish.ToModel();

            var json = JsonSerializer.Serialize((IControlModel)model, ProjectSerializer.Options);
            var loaded = JsonSerializer.Deserialize<IControlModel>(json, ProjectSerializer.Options);

            var liquishModel = Assert.IsType<LiquishModel>(loaded);
            Assert.Equal(9f, liquishModel.Scale.Y.Value.Value);
            Assert.Equal("#FF010203", liquishModel.ColorC.Value);
        }
    }
}
