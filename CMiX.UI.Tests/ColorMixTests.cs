using Avalonia.Media;
using CMiX.UI.Mathematics;
using Xunit;

namespace CMiX.UI.Tests
{
    public class ColorMixTests
    {
        private static readonly Color Dark = Color.FromRgb(34, 34, 36);
        private static readonly Color Amber = Color.FromRgb(233, 211, 73);

        [Fact]
        public void Lerp_AtTheEnds_GivesTheTwoColors()
        {
            Assert.Equal(Dark, ColorMix.Lerp(Dark, Amber, 0));
            Assert.Equal(Amber, ColorMix.Lerp(Dark, Amber, 1));
        }

        [Fact]
        public void Lerp_InTheMiddle_GivesTheRoundedAverageOfEachChannel()
        {
            var middle = ColorMix.Lerp(Color.FromRgb(0, 10, 255), Color.FromRgb(255, 11, 0), 0.5);

            Assert.Equal(Color.FromRgb(128, 11, 128), middle);
        }

        [Fact]
        public void Lerp_WithAnAmountOutsideZeroAndOne_StopsAtTheEnds()
        {
            Assert.Equal(Dark, ColorMix.Lerp(Dark, Amber, -2));
            Assert.Equal(Amber, ColorMix.Lerp(Dark, Amber, 3));
        }

        [Fact]
        public void Lerp_AlsoMixesTheAlphaChannel()
        {
            var result = ColorMix.Lerp(Color.FromArgb(0, 10, 20, 30), Color.FromArgb(200, 10, 20, 30), 0.5);

            Assert.Equal(100, result.A);
        }

        [Fact]
        public void Over_WithAStrength_IsTheColorOverTheBackground()
        {
            Assert.Equal(Dark, ColorMix.Over(Amber, Dark, 0));
            Assert.Equal(Amber, ColorMix.Over(Amber, Dark, 1));
            Assert.Equal(ColorMix.Lerp(Dark, Amber, 0.2), ColorMix.Over(Amber, Dark, 0.2));
        }
    }
}
