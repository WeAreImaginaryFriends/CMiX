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
        public void Create_AComposition_OpensItsPanels()
        {
            var composition = New<Composition>();

            Assert.True(composition.TextureModifierIsExpanded);
            Assert.True(composition.ModifierIsExpanded);
            Assert.True(composition.OutputSettingsIsExpanded);
        }

        [Fact]
        public void Create_AnEntity_OpensItsPanels()
        {
            var entity = New<Entity>();

            Assert.True(entity.TransformSRTIsExpanded);
            Assert.True(entity.ModifierManagerIsExpanded);
            Assert.True(entity.MaterialIsExpanded);
            Assert.True(entity.ColorPaletteManagerIsExpanded);
            Assert.True(entity.MeshIsExpanded);
        }

        [Fact]
        public void Create_ALight_OpensItsPanels()
        {
            var light = New<LightEntity>();

            Assert.True(light.TransformSRTIsExpanded);
            Assert.True(light.ModifierManagerIsExpanded);
        }
    }
}
