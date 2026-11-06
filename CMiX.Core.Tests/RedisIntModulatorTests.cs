using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // The values of real controls that a RedisIntModulator can bind. RedisModulatorTests holds the shared behaviour.
    public class RedisIntModulatorTests
    {
        private readonly IServiceProvider _provider = TestServiceProviderFactory.Create();

        private RedisIntModulator NewModulator() =>
            (RedisIntModulator)_provider.GetRequiredService<ControlFactory>().Create(typeof(RedisIntModulator));

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
