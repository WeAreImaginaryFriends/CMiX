using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class ModulatorCategoryTests
    {
        private static readonly Dictionary<Type, ModulatorCategory> Expected = new()
        {
            [typeof(LFOModulator)] = ModulatorCategory.Internal,
            [typeof(RandomModulator)] = ModulatorCategory.Internal,
            [typeof(FFTModulator)] = ModulatorCategory.Internal,
            [typeof(BeatLFOModulator)] = ModulatorCategory.Internal,
            [typeof(BeatRandomModulator)] = ModulatorCategory.Internal,
            [typeof(BeatStepModulator)] = ModulatorCategory.Internal,
            [typeof(IntegratorModulator)] = ModulatorCategory.Internal,
            [typeof(RedisFloatModulator)] = ModulatorCategory.External,
            [typeof(RedisIntModulator)] = ModulatorCategory.External,
            [typeof(RedisBoolModulator)] = ModulatorCategory.External,
            [typeof(TrackingModulator)] = ModulatorCategory.External,
            [typeof(LISAModulator)] = ModulatorCategory.External,
            [typeof(VisitorDataModulator)] = ModulatorCategory.External,
        };

        public static IEnumerable<object[]> ModulatorTypes => Expected.Select(pair => new object[] { pair.Key, pair.Value });

        [Theory]
        [MemberData(nameof(ModulatorTypes))]
        public void Category_OfAModulatorType_IsTheExpectedCategory(Type type, ModulatorCategory expected)
        {
            Assert.Equal(expected, ModulatorCategoryAttribute.Of(type));
        }

        // A new modulator type must be added to the table above, so its category is a decision.
        [Fact]
        public void Category_EveryConcreteModulatorType_HasAnEntryInTheTable()
        {
            var missing = typeof(IModulator).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && type.IsClass && typeof(IModulator).IsAssignableFrom(type))
                .Where(type => !Expected.ContainsKey(type))
                .Select(type => type.Name)
                .ToList();

            Assert.Empty(missing);
        }

        [Fact]
        public void Category_OnAnInstance_ReadsTheAttribute()
        {
            var provider = TestServiceProviderFactory.Create();
            var factory = provider.GetRequiredService<ControlFactory>();

            Assert.Equal(ModulatorCategory.External, ((IModulator)factory.Create(typeof(RedisFloatModulator))).Category);
            Assert.Equal(ModulatorCategory.Internal, ((IModulator)factory.Create(typeof(LFOModulator))).Category);
        }
    }
}
