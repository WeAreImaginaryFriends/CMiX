using CMiX.Core.Animations;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // A RedisIntModulator: one selected address of the project, one int output named Value, no gain.
    public class RedisIntModulatorTests
    {
        private readonly IServiceProvider _provider = TestServiceProviderFactory.Create();

        private RedisAddressRegistry Registry => _provider.GetRequiredService<RedisAddressRegistry>();

        private RedisIntModulator NewModulator() =>
            (RedisIntModulator)_provider.GetRequiredService<ControlFactory>().Create(typeof(RedisIntModulator));

        private RedisAddress AddAddress(string text)
        {
            Registry.AddressManager.AddItem(typeof(RedisAddress));
            var address = (RedisAddress)Registry.AddressManager.SelectedItem;
            address.Value = text;
            return address;
        }

        [Fact]
        public void Create_ANewModulator_SelectsNothing()
        {
            AddAddress("cmix:a");

            var modulator = NewModulator();

            Assert.Null(modulator.SelectedAddress);
            Assert.Null(modulator.SelectedAddressID.Value);
        }

        [Fact]
        public void Outputs_WithoutASelection_IsOneIntOutputNamedValue()
        {
            var modulator = NewModulator();

            var output = Assert.Single(modulator.Outputs);
            var addressed = Assert.IsType<AddressedOutput<int>>(output);
            Assert.IsAssignableFrom<ModulatorOutput<int>>(output);
            Assert.Equal("Value", addressed.Name);
            Assert.Equal("Value", addressed.Address);
        }

        [Fact]
        public void Outputs_WithASelection_KeepsTheNameAndShowsTheAddress()
        {
            var modulator = NewModulator();
            var address = AddAddress("cmix:a");

            modulator.SelectedAddress = address;

            var output = (AddressedOutput<int>)Assert.Single(modulator.Outputs);
            Assert.Equal("Value", output.Name);
            Assert.Equal("cmix:a", output.Address);
            Assert.Same(address, modulator.SelectedAddress);
        }

        [Fact]
        public void FromModel_OfToModel_KeepsTheSelection()
        {
            var modulator = NewModulator();
            var address = AddAddress("cmix:a");
            modulator.SelectedAddress = address;

            var copy = NewModulator();
            copy.FromModel(modulator.ToModel());

            Assert.Equal(modulator.ID, copy.ID);
            Assert.Same(address, copy.SelectedAddress);
        }

        [Fact]
        public void FromModel_AnUnknownAddressID_GivesNoSelectedAddress()
        {
            var modulator = NewModulator();
            var model = (RedisIntModulatorModel)modulator.ToModel();
            model.SelectedAddressID = new(Guid.NewGuid());

            modulator.FromModel(model);

            Assert.NotNull(modulator.SelectedAddressID.Value);
            Assert.Null(modulator.SelectedAddress);
        }

        [Fact]
        public void CanBind_TheSeekTargetOfASequencePlayer_IsTrue()
        {
            var player = (SequencePlayer)_provider.GetRequiredService<ControlFactory>().Create(typeof(SequencePlayer));
            var modulator = NewModulator();

            Assert.True(((IModulatorBindable)player.SeekTarget).CanBind(modulator.Outputs.Single()));
        }

        [Fact]
        public void CanBind_TheStrengthOfABlur_IsFalse()
        {
            var blur = (Blur)_provider.GetRequiredService<ControlFactory>().Create(typeof(Blur));
            var modulator = NewModulator();

            Assert.False(((IModulatorBindable)blur.Strength).CanBind(modulator.Outputs.Single()));
        }
    }
}
