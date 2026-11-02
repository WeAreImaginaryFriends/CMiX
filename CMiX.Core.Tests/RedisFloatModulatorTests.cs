using CMiX.Core.Animations;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // A RedisFloatModulator: one selected address of the project, one output named Value, one range.
    public class RedisFloatModulatorTests
    {
        private readonly IServiceProvider _provider = TestServiceProviderFactory.Create();

        private RedisAddressRegistry Registry => _provider.GetRequiredService<RedisAddressRegistry>();

        private RedisFloatModulator NewModulator() =>
            (RedisFloatModulator)_provider.GetRequiredService<ControlFactory>().Create(typeof(RedisFloatModulator));

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
        public void Outputs_WithoutASelection_IsOneFloatOutputNamedValue()
        {
            var modulator = NewModulator();

            var output = Assert.Single(modulator.Outputs);
            var addressed = Assert.IsType<AddressedOutput<float>>(output);
            Assert.IsAssignableFrom<ModulatorOutput<float>>(output);
            Assert.Equal("Value", addressed.Name);
            Assert.Equal("Value", addressed.Address);
        }

        [Fact]
        public void Outputs_WithASelection_KeepsTheNameAndShowsTheAddress()
        {
            var modulator = NewModulator();
            var address = AddAddress("cmix:a");

            modulator.SelectedAddress = address;

            var output = (AddressedOutput<float>)Assert.Single(modulator.Outputs);
            Assert.Equal("Value", output.Name);
            Assert.Equal("cmix:a", output.Address);
            Assert.Same(address, modulator.SelectedAddress);
        }

        [Fact]
        public void Range_ANewModulator_StartsAtZeroToOne()
        {
            var modulator = NewModulator();

            Assert.Equal(0.0f, modulator.Range.Minimum.Value);
            Assert.Equal(1.0f, modulator.Range.Maximum.Value);
        }

        [Fact]
        public void ResetAll_TheRange_GoesBackToZeroToOne()
        {
            var modulator = NewModulator();
            modulator.Range.Minimum.Value = -3.0f;
            modulator.Range.Maximum.Value = 3.0f;

            modulator.Range.ResetAllCommand.Execute(null);

            Assert.Equal(0.0f, modulator.Range.Minimum.Value);
            Assert.Equal(1.0f, modulator.Range.Maximum.Value);
        }

        [Fact]
        public void FromModel_OfToModel_KeepsTheSelectionAndTheRange()
        {
            var modulator = NewModulator();
            var address = AddAddress("cmix:a");
            modulator.SelectedAddress = address;
            modulator.Range.Minimum.Value = 0.25f;
            modulator.Range.Maximum.Value = 2.5f;

            var copy = NewModulator();
            copy.FromModel(modulator.ToModel());

            Assert.Equal(modulator.ID, copy.ID);
            Assert.Same(address, copy.SelectedAddress);
            Assert.Equal(0.25f, copy.Range.Minimum.Value);
            Assert.Equal(2.5f, copy.Range.Maximum.Value);
        }

        [Fact]
        public void FromModel_AnUnknownAddressID_GivesNoSelectedAddress()
        {
            var modulator = NewModulator();
            var model = (RedisFloatModulatorModel)modulator.ToModel();
            model.SelectedAddressID = new(Guid.NewGuid());

            modulator.FromModel(model);

            Assert.NotNull(modulator.SelectedAddressID.Value);
            Assert.Null(modulator.SelectedAddress);
        }

        [Fact]
        public void SelectedAddress_TwoModulators_CanSelectDifferentAddresses()
        {
            var first = NewModulator();
            var second = NewModulator();
            var a = AddAddress("cmix:a");
            var b = AddAddress("cmix:b");

            first.SelectedAddress = a;
            second.SelectedAddress = b;

            Assert.Same(a, first.SelectedAddress);
            Assert.Same(b, second.SelectedAddress);
            Assert.Equal("cmix:a", ((AddressedOutput<float>)first.Outputs[0]).Address);
            Assert.Equal("cmix:b", ((AddressedOutput<float>)second.Outputs[0]).Address);
        }
    }
}
