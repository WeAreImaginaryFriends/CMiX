using System;
using Avalonia.Media;
using CMiX.UI.Mathematics;
using Xunit;

namespace CMiX.UI.Tests
{
    public class ColorMixTests
    {
        private static readonly Color Dark = Color.FromRgb(34, 34, 36);
        private static readonly Color Amber = Color.FromRgb(233, 211, 73);
        private static readonly Color Magenta = Color.FromRgb(220, 116, 215);
        private static readonly Color Violet = Color.FromRgb(154, 139, 228);

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
        public void Luminance_IsZeroForBlack_OneForWhite_AndGreenIsBrighterThanBlue()
        {
            Assert.Equal(0, ColorMix.Luminance(Color.FromRgb(0, 0, 0)), 6);
            Assert.Equal(1, ColorMix.Luminance(Color.FromRgb(255, 255, 255)), 6);
            Assert.True(ColorMix.Luminance(Color.FromRgb(0, 255, 0)) > ColorMix.Luminance(Color.FromRgb(0, 0, 255)));
        }

        [Fact]
        public void Contrast_IsTwentyOneForBlackOnWhite_OneForTheSameColor_AndDoesNotDependOnTheOrder()
        {
            var black = Color.FromRgb(0, 0, 0);
            var white = Color.FromRgb(255, 255, 255);

            Assert.Equal(21, ColorMix.Contrast(black, white), 6);
            Assert.Equal(21, ColorMix.Contrast(white, black), 6);
            Assert.Equal(1, ColorMix.Contrast(Amber, Amber), 6);
        }

        [Theory]
        [InlineData(0.03)]
        [InlineData(0.075)]
        [InlineData(0.18)]
        public void WithLuminance_GivesTheLuminanceAskedFor_ForAnyHue(double luminance)
        {
            foreach (var color in new[] { Amber, Magenta, Violet, Color.FromRgb(230, 103, 86), Color.FromRgb(96, 199, 162) })
                Assert.InRange(ColorMix.Luminance(ColorMix.WithLuminance(color, luminance, 0.55, 0.85)), luminance - 0.004, luminance + 0.004);
        }

        [Fact]
        public void WithLuminance_KeepsTheHue_AndKeepsTheSaturationInTheLimits()
        {
            var tone = ColorMix.WithLuminance(Amber, 0.075, 0.55, 0.85);

            // A yellow stays yellow: red and green are close, blue is the weakest.
            Assert.True(tone.R > tone.B && tone.G > tone.B);
            var low = ColorMix.WithLuminance(Color.FromRgb(120, 118, 119), 0.075, 0.55, 0.85);
            Assert.True(Math.Max(low.R, Math.Max(low.G, low.B)) - Math.Min(low.R, Math.Min(low.G, low.B)) > 20);
        }

        [Fact]
        public void WithLuminance_IsStrongerInColorThanAMixWithTheBackground_AtTheSameBrightness()
        {
            var mixed = ColorMix.Over(Magenta, Dark, 0.20);
            var tone = ColorMix.WithLuminance(Magenta, ColorMix.Luminance(mixed), 0.55, 0.85);

            static int Spread(Color color) => Math.Max(color.R, Math.Max(color.G, color.B)) - Math.Min(color.R, Math.Min(color.G, color.B));
            Assert.True(Spread(tone) > Spread(mixed));
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
