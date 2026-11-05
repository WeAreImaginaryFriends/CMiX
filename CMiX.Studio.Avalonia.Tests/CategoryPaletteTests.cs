using System.Collections.Generic;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Mathematics;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tones of a kind of row: the fills of the states, and the colors of the count squares.
    public class CategoryPaletteTests
    {
        private static readonly Color Background = Color.FromRgb(34, 34, 36);
        private static readonly Color Amber = Color.FromRgb(233, 211, 73);
        private static readonly Color Magenta = Color.FromRgb(220, 116, 215);
        private static readonly Color Violet = Color.FromRgb(159, 133, 236);
        private static readonly Color Text = Color.FromRgb(224, 224, 224);
        private static readonly Color White = Color.FromRgb(255, 255, 255);

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

        private static double LuminanceOf(Color color) => ColorMix.Luminance(color);

        [Fact]
        public void TheFills_AreTheColorOfTheKindOverTheBackground_WithOneStrengthForEachState()
        {
            var tone = Palette().For(TreeCategory.Light);

            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillStrength), tone.Fill);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillHoverStrength), tone.FillHover);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillSelectedStrength), tone.FillSelected);
        }

        [Fact]
        public void TheCountSquares_AreTheColorOfTheKindOverTheBackground_WithOneStrengthForEachState()
        {
            var tone = Palette().For(TreeCategory.Light);

            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.KindStrength), tone.Kind);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.KindHoverStrength), tone.KindHover);
            Assert.Equal(Amber, tone.KindSelected);
        }

        [Fact]
        public void EachState_IsBrighterThanTheOneBefore()
        {
            var tone = Palette().For(TreeCategory.Light);

            Assert.True(LuminanceOf(tone.Fill) < LuminanceOf(tone.FillHover));
            Assert.True(LuminanceOf(tone.FillHover) < LuminanceOf(tone.FillSelected));
            Assert.True(LuminanceOf(tone.Kind) < LuminanceOf(tone.KindHover));
            Assert.True(LuminanceOf(tone.KindHover) < LuminanceOf(tone.KindSelected));
        }

        [Fact]
        public void ARowDeeperByAChangeOfKind_IsWeaker_ByTheLevelFactor_DownToTheFloor()
        {
            var palette = Palette();

            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillStrength * CategoryPalette.LevelFactor), palette.For(TreeCategory.Light, kindDepth: 1).Fill);
            Assert.True(LuminanceOf(palette.For(TreeCategory.Light, kindDepth: 2).Fill) < LuminanceOf(palette.For(TreeCategory.Light, kindDepth: 1).Fill));
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillFloor), palette.For(TreeCategory.Light, kindDepth: 40).Fill);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillHoverFloor), palette.For(TreeCategory.Light, kindDepth: 40).FillHover);
            Assert.Equal(ColorMix.Over(Amber, Background, CategoryPalette.FillSelectedFloor), palette.For(TreeCategory.Light, kindDepth: 40).FillSelected);
        }

        [Fact]
        public void ADeepRow_IsStillAColor_AndEachStateIsStillBrighterThanTheOneBefore()
        {
            var tone = Palette().For(TreeCategory.Light, kindDepth: 40);

            Assert.NotEqual(Background, tone.Fill);
            Assert.True(LuminanceOf(tone.Fill) < LuminanceOf(tone.FillHover));
            Assert.True(LuminanceOf(tone.FillHover) < LuminanceOf(tone.FillSelected));
        }

        [Fact]
        public void TheComposableDepth_ChangesTheHue_AndNotTheStrength()
        {
            var palette = Palette();

            Assert.NotEqual(palette.For(TreeCategory.Composable, 0).Fill, palette.For(TreeCategory.Composable, 3).Fill);
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
        }

        // The text must stay readable on every fill: the light text of the theme at rest and when hovered, white when selected.
        [AvaloniaFact]
        public void TheTextIsReadable_OnEveryKindAtEveryLevelAndState()
        {
            var palette = CategoryPalette.FromApplication();

            foreach (var category in System.Enum.GetValues<TreeCategory>())
            {
                for (var composable = 0; composable <= CategoryPalette.LastComposableLevel; composable++)
                {
                    for (var kind = 0; kind <= 6; kind++)
                    {
                        var tone = palette.For(category, composable, kind);
                        Assert.True(ColorMix.Contrast(Text, tone.Fill) >= 4.5, $"{category} {composable}/{kind} at rest: {ColorMix.Contrast(Text, tone.Fill):0.0}");
                        Assert.True(ColorMix.Contrast(Text, tone.FillHover) >= 4.5, $"{category} {composable}/{kind} hovered: {ColorMix.Contrast(Text, tone.FillHover):0.0}");
                        Assert.True(ColorMix.Contrast(White, tone.FillSelected) >= 4.5, $"{category} {composable}/{kind} selected: {ColorMix.Contrast(White, tone.FillSelected):0.0}");
                    }
                }
            }
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
