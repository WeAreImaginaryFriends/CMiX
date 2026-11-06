using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // The range of a RedisFloatModulator. RedisModulatorTests holds the shared behaviour.
    public class RedisFloatModulatorTests
    {
        private readonly IServiceProvider _provider = TestServiceProviderFactory.Create();

        private RedisFloatModulator NewModulator() =>
            (RedisFloatModulator)_provider.GetRequiredService<ControlFactory>().Create(typeof(RedisFloatModulator));

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
        public void FromModel_OfToModel_KeepsTheRange()
        {
            var modulator = NewModulator();
            modulator.Range.Minimum.Value = 0.25f;
            modulator.Range.Maximum.Value = 2.5f;

            var copy = NewModulator();
            copy.FromModel(modulator.ToModel());

            Assert.Equal(0.25f, copy.Range.Minimum.Value);
            Assert.Equal(2.5f, copy.Range.Maximum.Value);
        }
    }
}
