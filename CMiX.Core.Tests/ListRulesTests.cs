using CMiX.Core.Compositing;
using CMiX.Core.Modulation.Modifiers;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using Xunit;

namespace CMiX.Core.Tests
{
    // The rule of which kinds of item an owner type can hold.
    public class ListRulesTests
    {
        [Theory]
        [InlineData(typeof(Layer), typeof(Entity), true)]
        [InlineData(typeof(Layer), typeof(Camera), true)]
        [InlineData(typeof(Entity), typeof(TranslateModifier), true)]
        [InlineData(typeof(Entity), typeof(ScaleModifier), true)]
        [InlineData(typeof(Layer), typeof(TranslateModifier), false)]
        [InlineData(typeof(Composition), typeof(AlphaModifier), true)]
        [InlineData(typeof(Entity), typeof(Image), true)]
        [InlineData(typeof(Layer), typeof(Image), false)]
        [InlineData(typeof(Layer), typeof(Blur), true)]
        [InlineData(typeof(Composition), typeof(Blur), true)]
        [InlineData(typeof(Entity), typeof(Blur), false)]
        [InlineData(typeof(Blur), typeof(LFOModulator), true)]
        [InlineData(typeof(Blur), typeof(RandomModulator), true)]
        [InlineData(typeof(Layer), typeof(LFOModulator), false)]
        public void CanHold_AnOwnerAndAnItemType_GivesTheRule(Type ownerType, Type itemType, bool expected)
        {
            Assert.Equal(expected, ListRules.CanHold(ownerType, itemType));
        }

        [Fact]
        public void CanHold_ANullType_IsFalse()
        {
            Assert.False(ListRules.CanHold(null, typeof(Entity)));
            Assert.False(ListRules.CanHold(typeof(Layer), null));
        }
    }
}
