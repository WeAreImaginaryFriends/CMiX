using Avalonia;
using Avalonia.Headless.XUnit;
using Xunit;

namespace CMiX.UI.Tests
{
    // XAML cannot add two tokens. These tests keep the tokens of Geometry.axaml that are sums of other tokens in step.
    public class GeometryTokenTests
    {
        private static double Size(string key) => Tokens.Get<double>(key);

        private static Thickness Space(string key) => Tokens.Get<Thickness>(key);

        [AvaloniaFact]
        public void ScrollBarSize_IsTheThicknessPlusTwoMargins()
        {
            var margin = Space("ScrollBarMargin");

            Assert.Equal(Size("ScrollBarSize"), Size("ScrollBarThickness") + margin.Left + margin.Right);
        }

        [AvaloniaFact]
        public void ScrollBarGutter_IsTheScrollBarSizeAtTheRight()
        {
            Assert.Equal(new Thickness(0, 0, Size("ScrollBarSize"), 0), Space("ScrollBarGutter"));
        }

        [AvaloniaFact]
        public void TreeIndentMargin_IsTheTreeIndentAtTheLeft()
        {
            Assert.Equal(new Thickness(Size("TreeIndent"), 0, 0, 0), Space("TreeIndentMargin"));
        }

        [AvaloniaFact]
        public void TreeSpaces_AreSpacingTokens()
        {
            var md = Size("SpacingMD");
            var sm = Size("SpacingSM");

            Assert.Equal(new Thickness(md, md, 0, md), Space("TreeMargin"));
            Assert.Equal(new Thickness(Size("SpacingXS"), 0, 0, 0), Space("TreeRowContentMargin"));
            Assert.Equal(new Thickness(sm, 0), Space("TreeHeaderMargin"));
            Assert.Equal(new Thickness(sm), Space("TreeToolbarPadding"));
        }
    }
}
