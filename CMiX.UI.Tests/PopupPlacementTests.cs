using Avalonia;
using CMiX.UI.Popups;
using Xunit;

namespace CMiX.UI.Tests
{
    // The rule that places a card next to an anchor: the four sides, the flip, the margin and the tip.
    public class PopupPlacementTests
    {
        private static readonly Rect Screen = new(0, 0, 1000, 600);
        private static readonly Size Card = new(200, 60);

        private static PlacementRequest Request(Rect anchor, PopupSide side, Rect? bounds = null, Size? card = null) =>
            new(anchor, card ?? Card, bounds ?? Screen, side);

        [Fact]
        public void Place_BottomWithRoom_PutsTheCardBelowTheAnchorAtTheGap()
        {
            var result = PopupPlacement.Place(Request(new Rect(400, 100, 80, 24), PopupSide.Bottom));

            Assert.Equal(PopupSide.Bottom, result.Side);
            Assert.False(result.Flipped);
            Assert.Equal(new Point(340, 132), result.Position);
        }

        [Fact]
        public void Place_TopWithRoom_PutsTheCardAboveTheAnchorAtTheGap()
        {
            var result = PopupPlacement.Place(Request(new Rect(400, 300, 80, 24), PopupSide.Top));

            Assert.Equal(PopupSide.Top, result.Side);
            Assert.Equal(new Point(340, 232), result.Position);
        }

        [Fact]
        public void Place_RightWithRoom_PutsTheCardAtTheRightOfTheAnchorAtTheGap()
        {
            var result = PopupPlacement.Place(Request(new Rect(100, 200, 40, 24), PopupSide.Right));

            Assert.Equal(PopupSide.Right, result.Side);
            Assert.Equal(new Point(148, 182), result.Position);
        }

        [Fact]
        public void Place_LeftWithRoom_PutsTheCardAtTheLeftOfTheAnchorAtTheGap()
        {
            var result = PopupPlacement.Place(Request(new Rect(600, 200, 40, 24), PopupSide.Left));

            Assert.Equal(PopupSide.Left, result.Side);
            Assert.Equal(new Point(392, 182), result.Position);
        }

        [Fact]
        public void Place_NoRoomBelow_FlipsAbove()
        {
            var result = PopupPlacement.Place(Request(new Rect(400, 560, 80, 24), PopupSide.Bottom));

            Assert.Equal(PopupSide.Top, result.Side);
            Assert.True(result.Flipped);
            Assert.Equal(492, result.Position.Y);
        }

        [Fact]
        public void Place_NoRoomAbove_FlipsBelow()
        {
            var result = PopupPlacement.Place(Request(new Rect(400, 10, 80, 24), PopupSide.Top));

            Assert.Equal(PopupSide.Bottom, result.Side);
            Assert.True(result.Flipped);
            Assert.Equal(42, result.Position.Y);
        }

        [Fact]
        public void Place_NoRoomAtTheRight_FlipsToTheLeft()
        {
            var result = PopupPlacement.Place(Request(new Rect(940, 200, 40, 24), PopupSide.Right));

            Assert.Equal(PopupSide.Left, result.Side);
            Assert.True(result.Flipped);
            Assert.Equal(732, result.Position.X);
        }

        [Fact]
        public void Place_NoRoomAtTheLeft_FlipsToTheRight()
        {
            var result = PopupPlacement.Place(Request(new Rect(10, 200, 40, 24), PopupSide.Left));

            Assert.Equal(PopupSide.Right, result.Side);
            Assert.True(result.Flipped);
            Assert.Equal(58, result.Position.X);
        }

        [Fact]
        public void Place_RoomOnNeitherSide_TakesTheSideWithMoreRoomAndStaysInTheBounds()
        {
            var bounds = new Rect(0, 0, 300, 100);
            var result = PopupPlacement.Place(Request(new Rect(100, 60, 80, 24), PopupSide.Bottom, bounds));

            Assert.Equal(PopupSide.Top, result.Side);
            Assert.True(result.Position.Y >= 8);
            Assert.True(result.Position.Y + Card.Height <= 100 - 8 || result.Position.Y == 8);
        }

        [Fact]
        public void Place_NearTheRightEdge_StopsAtTheMarginAndKeepsTheTipOnTheTarget()
        {
            var request = Request(new Rect(940, 100, 40, 24), PopupSide.Bottom) with { Along = 975, AlignOffset = 28 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(1000 - 8 - 200, result.Position.X);
            Assert.Equal(975 - result.Position.X, result.TipOffset);
        }

        [Fact]
        public void Place_NearTheLeftEdge_StopsAtTheMargin()
        {
            var request = Request(new Rect(2, 100, 40, 24), PopupSide.Bottom) with { Along = 5, AlignOffset = 28 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(8, result.Position.X);
            Assert.Equal(6, result.TipOffset);
        }

        [Fact]
        public void Place_InTheBottomRightCorner_FlipsAndStopsAtTheMargin()
        {
            var request = Request(new Rect(950, 570, 40, 24), PopupSide.Bottom) with { Along = 980, AlignOffset = 28 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(PopupSide.Top, result.Side);
            Assert.Equal(new Point(792, 502), result.Position);
        }

        [Fact]
        public void Place_WithAPointAlong_PointsTheTipAtThePointNotAtTheMiddleOfTheAnchor()
        {
            var request = Request(new Rect(400, 100, 300, 24), PopupSide.Bottom) with { Along = 450, AlignOffset = 28 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(422, result.Position.X);
            Assert.Equal(28, result.TipOffset);
        }

        [Fact]
        public void Place_WithAnAlignOffset_LinesUpThatPointOfTheCardWithTheTarget()
        {
            var request = Request(new Rect(100, 200, 40, 24), PopupSide.Right) with { AlignOffset = 18 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(212 - 18, result.Position.Y);
            Assert.Equal(18, result.TipOffset);
        }

        [Fact]
        public void Place_WithoutAnAlignOffset_CentersTheCardOnTheTarget()
        {
            var result = PopupPlacement.Place(Request(new Rect(400, 100, 80, 24), PopupSide.Bottom));

            Assert.Equal(440 - 100, result.Position.X);
            Assert.Equal(100, result.TipOffset);
        }

        [Fact]
        public void Place_ATargetNearACornerOfTheCard_KeepsTheTipInsideTheInset()
        {
            var request = Request(new Rect(400, 100, 80, 24), PopupSide.Bottom) with { Along = 400, AlignOffset = 0, TipInset = 10 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(10, result.TipOffset);
        }

        [Fact]
        public void Place_BoundsAwayFromTheOrigin_UseTheBoundsNotTheOrigin()
        {
            var second = new Rect(-1920, 0, 1920, 1080);
            var request = Request(new Rect(-30, 100, 20, 24), PopupSide.Bottom, second) with { Along = -20, AlignOffset = 28 };

            var result = PopupPlacement.Place(request);

            Assert.Equal(-8 - 200, result.Position.X);
            Assert.Equal(132, result.Position.Y);
        }

        [Fact]
        public void Place_ACardLargerThanTheBounds_StartsAtTheMargin()
        {
            var bounds = new Rect(0, 0, 150, 40);
            var result = PopupPlacement.Place(Request(new Rect(50, 10, 20, 10), PopupSide.Bottom, bounds));

            Assert.Equal(8, result.Position.X);
            Assert.Equal(8, result.Position.Y);
        }

        [Theory]
        [InlineData(PopupSide.Bottom, PopupSide.Top)]
        [InlineData(PopupSide.Top, PopupSide.Bottom)]
        [InlineData(PopupSide.Right, PopupSide.Left)]
        [InlineData(PopupSide.Left, PopupSide.Right)]
        public void TipEdge_ForEachSide_FacesTheAnchor(PopupSide side, PopupSide edge)
        {
            var result = PopupPlacement.Place(Request(new Rect(480, 280, 40, 24), side));

            Assert.Equal(side, result.Side);
            Assert.Equal(edge, result.TipEdge);
        }
    }
}
