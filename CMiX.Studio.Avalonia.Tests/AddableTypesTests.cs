using System.Globalization;
using CMiX.Core.Modulation.Modifiers;
using CMiX.Core.Modulation.Modulators;
using CMiX.Studio.Avalonia.Converters;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Xunit;
using Image = CMiX.Core.Texturing.Sources.Image;

namespace CMiX.Studio.Avalonia.Tests
{
    // AddableTypes.LabelOf gives the label of a type in the menus of the tree and in the add panels.
    public class AddableTypesTests
    {
        [Fact]
        public void LabelOf_AModifierType_DropsTheSuffix()
        {
            Assert.Equal("Translate", AddableTypes.LabelOf(typeof(TranslateModifier)));
        }

        [Fact]
        public void LabelOf_AModulatorType_DropsTheSuffix()
        {
            Assert.Equal("LFO", AddableTypes.LabelOf(typeof(LFOModulator)));
        }

        [Fact]
        public void LabelOf_ATextureType_KeepsTheName()
        {
            Assert.Equal("Image", AddableTypes.LabelOf(typeof(Image)));
        }

        [Fact]
        public void LabelOf_ATypeWithAModifierLabel_UsesTheLabel()
        {
            Assert.Equal("Visibility", AddableTypes.LabelOf(typeof(ComposableVisibilityModifier)));
        }

        [Fact]
        public void LabelOf_ATypeThatIsNoModifier_KeepsTheSuffix()
        {
            Assert.Equal("Plain Modifier", AddableTypes.LabelOf(typeof(PlainModifier)));
        }

        [Fact]
        public void TypeNameToDisplayLabelConverter_AnyType_GivesTheLabelOfAddableTypes()
        {
            var converter = TypeNameToDisplayLabelConverter.Instance;

            foreach (var type in new[] { typeof(TranslateModifier), typeof(LFOModulator), typeof(Image), typeof(PlainModifier) })
                Assert.Equal(AddableTypes.LabelOf(type), converter.Convert(type, typeof(string), null, CultureInfo.InvariantCulture));
        }

        private sealed class PlainModifier
        {
        }
    }
}
