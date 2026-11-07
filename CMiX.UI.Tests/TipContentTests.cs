using CMiX.UI.Popups;
using Xunit;

namespace CMiX.UI.Tests
{
    // The lines of a tooltip. Only the title is needed.
    public class TipContentTests
    {
        [Fact]
        public void Create_WithATitleOnly_HasNoOtherLine()
        {
            var tip = new TipContent("Add");

            Assert.Equal("Add", tip.Title);
            Assert.Null(tip.Shortcut);
            Assert.Null(tip.Description);
            Assert.Null(tip.Reason);
            Assert.Null(tip.Footer);
        }

        [Fact]
        public void ToString_OfATip_IsItsTitle()
        {
            Assert.Equal("Remove", new TipContent("Remove", "Del", "Takes the row out.").ToString());
        }

        [Fact]
        public void Equals_TwoTipsWithTheSameLines_AreEqual()
        {
            Assert.Equal(new TipContent("Add", "Shift+A"), new TipContent("Add", "Shift+A"));
            Assert.NotEqual(new TipContent("Add", "Shift+A"), new TipContent("Add"));
        }
    }
}
