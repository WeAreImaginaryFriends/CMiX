using System.Collections.Generic;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Mathematics;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tones of a kind of row: the color of the kind, over the background, for each state.
    public class CategoryPaletteTests
    {
        private static readonly Color Background = Color.FromRgb(34, 34, 36);
        private static readonly Color Amber = Color.FromRgb(233, 211, 73);
        private static readonly Color Magenta = Color.FromRgb(220, 116, 215);
        private static readonly Color Violet = Color.FromRgb(159, 133, 236);

        private static CategoryPalette Palette()
        {
            var colors = new Dictionary<string, Color>
            {
                ["CategoryLight"] = Amber,
                ["CategoryComposable"] = Magenta,
                ["CategoryComposableLevel3"] = Violet
            };

            return new CategoryPalette(key => colors[key], Background);
        }

        [Fact]
        public void ATone_IsTheColorOverTheBackground_WithOneStrengthForEachState()
        {
            var tone = Palette().For(TreeCategory.Light);

            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillStrength), tone.Fill);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillHoverStrength), tone.FillHover);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillSelectedStrength), tone.FillSelected);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.KindStrength), tone.Kind);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.KindHoverStrength), tone.KindHover);
            Assert.Equal(Amber, tone.KindSelected);
        }

        [Fact]
        public void EachState_IsBrighterThanTheOneBefore()
        {
            var tone = Palette().For(TreeCategory.Light);

            static int Sum(Color color) => color.R + color.G + color.B;
            Assert.True(Sum(tone.Fill) < Sum(tone.FillHover));
            Assert.True(Sum(tone.FillHover) < Sum(tone.FillSelected));
            Assert.True(Sum(tone.Kind) < Sum(tone.KindHover));
            Assert.True(Sum(tone.KindHover) < Sum(tone.KindSelected));
        }

        [Theory]
        [InlineData(TreeCategory.Light, 0, "CategoryLight")]
        [InlineData(TreeCategory.Light, 5, "CategoryLight")]
        [InlineData(TreeCategory.Composable, 0, "CategoryComposable")]
        [InlineData(TreeCategory.Composable, 1, "CategoryComposableLevel1")]
        [InlineData(TreeCategory.Composable, 3, "CategoryComposableLevel3")]
        [InlineData(TreeCategory.Composable, 9, "CategoryComposableLevel3")]
        public void TheColorKey_FollowsTheKind_AndOnlyAComposableChangesWithItsDepth(TreeCategory category, int depth, string expected)
        {
            Assert.Equal(expected, CategoryPalette.ColorKey(category, depth));
        }

        [Fact]
        public void ADeepComposable_HasTheColorOfTheLastLevel()
        {
            var palette = Palette();

            Assert.Equal(palette.For(TreeCategory.Composable, 3), palette.For(TreeCategory.Composable, 7));
            Assert.NotEqual(palette.For(TreeCategory.Composable, 0), palette.For(TreeCategory.Composable, 3));
        }

        [AvaloniaFact]
        public void ThePaletteOfTheApplication_HasATonePerKindAndLevel()
        {
            var palette = CategoryPalette.FromApplication();

            foreach (var category in System.Enum.GetValues<TreeCategory>())
                Assert.NotEqual(default, palette.For(category).Fill);

            for (var depth = 0; depth <= CategoryPalette.LastComposableLevel; depth++)
                Assert.NotEqual(default, palette.For(TreeCategory.Composable, depth).Fill);
        }
    }
}
