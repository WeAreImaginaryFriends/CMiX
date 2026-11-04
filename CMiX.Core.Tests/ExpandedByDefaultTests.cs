using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Rendering.Lights;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // The panels of an item are open when the item is new, so the editor shows everything.
    public class ExpandedByDefaultTests
    {
        private static T New<T>() where T : class =>
            (T)TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>().Create(typeof(T));

        [Fact]
        public void ANewComposition_HasItsPanelsOpen()
        {
            var composition = New<Composition>();

            Assert.True(composition.TextureModifierIsExpanded);
            Assert.True(composition.ModifierIsExpanded);
            Assert.True(composition.OutputSettingsIsExpanded);
        }

        [Fact]
        public void ANewEntity_HasItsPanelsOpen()
        {
            var entity = New<Entity>();

            Assert.True(entity.TransformSRTIsExpanded);
            Assert.True(entity.ModifierManagerIsExpanded);
            Assert.True(entity.MaterialIsExpanded);
            Assert.True(entity.ColorPaletteManagerIsExpanded);
            Assert.True(entity.MeshIsExpanded);
        }

        [Fact]
        public void ANewLight_HasItsPanelsOpen()
        {
            var light = New<LightEntity>();

            Assert.True(light.TransformSRTIsExpanded);
            Assert.True(light.ModifierManagerIsExpanded);
        }
    }
}
