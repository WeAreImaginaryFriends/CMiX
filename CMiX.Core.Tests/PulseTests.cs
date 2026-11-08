using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class PulseTests
    {
        private static Pulse CreatePulse()
        {
            var provider = TestServiceProviderFactory.Create();
            return (Pulse)provider.GetRequiredService<ControlFactory>().Create(typeof(Pulse));
        }

        [Fact]
        public void Create_NewPulse_HasTheDefaultValues()
        {
            var pulse = CreatePulse();

            Assert.Equal(1.0f, pulse.Center.Value);
            Assert.Equal(1.0f, pulse.SlopeWidth.Value);
            Assert.Equal(1.0f, pulse.CenterWidth.Value);
            Assert.False(pulse.Invert.Value);
            Assert.Equal(PulseChannel.Red, pulse.Channel.Value);
            Assert.Equal(3, pulse.Bindables.Count);
        }

        [Fact]
        public void ModulatorManager_AddItem_AddsARandomModulator()
        {
            var pulse = CreatePulse();

            pulse.ModulatorManager.AddItem(typeof(RandomModulator));

            Assert.Single(pulse.ModulatorManager.ManagerData.Items);
            Assert.IsType<RandomModulator>(pulse.ModulatorManager.ManagerData.Items[0]);
        }

        [Fact]
        public void ModulatorManager_MoveItem_KeepsTheBindings()
        {
            var pulse = CreatePulse();

            pulse.ModulatorManager.AddItem(typeof(BeatRandomModulator));
            var mod1 = (BeatRandomModulator)pulse.ModulatorManager.ManagerData.Items[0];
            pulse.ModulatorManager.AddItem(typeof(BeatRandomModulator));
            var mod2 = (BeatRandomModulator)pulse.ModulatorManager.ManagerData.Items[1];

            pulse.Center.SetModulatorCommand.Execute(new ModulatorOutputSelection(mod1, mod1.Outputs[0]));
            pulse.SlopeWidth.SetModulatorCommand.Execute(new ModulatorOutputSelection(mod2, mod2.Outputs[0]));

            pulse.ModulatorManager.ManagerData.Items.Move(0, 1);

            Assert.Equal(mod1.ID, pulse.Center.ModulatorID);
            Assert.Same(mod1, pulse.Center.BoundModulator);
            Assert.Equal(mod2.ID, pulse.SlopeWidth.ModulatorID);
            Assert.Same(mod2, pulse.SlopeWidth.BoundModulator);
        }

        [Fact]
        public void ModulatorManager_RemoveItem_UnbindsEveryValueThatUsesIt()
        {
            var pulse = CreatePulse();
            pulse.ModulatorManager.AddItem(typeof(RandomModulator));
            var randomModulator = (RandomModulator)pulse.ModulatorManager.ManagerData.Items[0];

            pulse.Center.SetModulatorCommand.Execute(new ModulatorOutputSelection(randomModulator, randomModulator.Outputs[0]));
            pulse.SlopeWidth.SetModulatorCommand.Execute(new ModulatorOutputSelection(randomModulator, randomModulator.Outputs[0]));

            pulse.ModulatorManager.RemoveItem(randomModulator);

            Assert.Null(pulse.Center.ModulatorID);
            Assert.Null(pulse.Center.BoundModulator);
            Assert.Null(pulse.SlopeWidth.ModulatorID);
            Assert.Null(pulse.SlopeWidth.BoundModulator);
        }

        [Fact]
        public void FromModel_OfToModel_KeepsEveryValue()
        {
            var pulse = CreatePulse();
            pulse.Center.Value = 0.5f;
            pulse.SlopeWidth.Value = 0.25f;
            pulse.CenterWidth.Value = 0.75f;
            pulse.Invert.Value = true;
            pulse.Channel.Value = PulseChannel.Alpha;

            var model = pulse.ToModel();
            var reloaded = CreatePulse();
            reloaded.FromModel(model);

            Assert.Equal(0.5f, reloaded.Center.Value);
            Assert.Equal(0.25f, reloaded.SlopeWidth.Value);
            Assert.Equal(0.75f, reloaded.CenterWidth.Value);
            Assert.True(reloaded.Invert.Value);
            Assert.Equal(PulseChannel.Alpha, reloaded.Channel.Value);
        }
    }
}
