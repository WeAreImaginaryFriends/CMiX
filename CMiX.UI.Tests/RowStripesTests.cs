using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using CMiX.UI.Controls.Parts;
using Xunit;

namespace CMiX.UI.Tests
{
    // The bands behind the rows: one row plus one space high, two brushes that alternate, started at the top of the rows.
    public class RowStripesTests
    {
        private static readonly IBrush First = new SolidColorBrush(Colors.DarkGray);
        private static readonly IBrush Second = new SolidColorBrush(Colors.Gray);

        // 20 high rows with 5 between them: a pitch of 25. The rows start 10 below the top of the control.
        private static (Window Window, RowStripes Stripes, StackPanel Rows) Show(double rowsTop = 10, bool withRows = true)
        {
            var rows = new StackPanel { Spacing = 5, Margin = new Thickness(0, rowsTop, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            for (var i = 0; i < 4; i++)
                rows.Children.Add(new Border { Height = 20 });

            var stripes = new RowStripes { RowHeight = 20, RowSpacing = 5, FirstBrush = First, SecondBrush = Second, Rows = withRows ? rows : null };
            var window = new Window { Content = new Grid { Children = { stripes, rows } }, Width = 200, Height = 100 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, stripes, rows);
        }

        [AvaloniaFact]
        public void TheBands_StartAtTheTopOfTheRows_AndGoOnAboveThem()
        {
            var (_, stripes, _) = Show(rowsTop: 10);

            var bands = stripes.Bands();

            // The first row is at 10. The band above it ends at 10: it starts at -15.
            Assert.Equal(-15, bands[0].Band.Top);
            Assert.Equal(10, bands[1].Band.Top);
            Assert.Equal(35, bands[2].Band.Top);
            Assert.All(bands, band => Assert.Equal(25, band.Band.Height));
            Assert.All(bands, band => Assert.Equal(stripes.Bounds.Width, band.Band.Width));
        }

        [AvaloniaFact]
        public void TheBands_AlternateTheBrushes_ThatOfTheFirstRowIsTheFirstBrush()
        {
            var (_, stripes, _) = Show(rowsTop: 10);

            var bands = stripes.Bands();

            Assert.Same(Second, bands[0].Brush);
            Assert.Same(First, bands[1].Brush);
            Assert.Same(Second, bands[2].Brush);
            Assert.Same(First, bands[3].Brush);
        }

        [AvaloniaFact]
        public void TheBands_FillTheWholeHeightOfTheControl_AlsoUnderTheLastRow()
        {
            var (_, stripes, _) = Show(rowsTop: 0);

            var bands = stripes.Bands();

            Assert.True(bands[^1].Band.Bottom >= stripes.Bounds.Height);
            Assert.True(bands[0].Band.Top <= 0);
            for (var i = 1; i < bands.Count; i++)
                Assert.Equal(bands[i - 1].Band.Bottom, bands[i].Band.Top);
        }

        [AvaloniaFact]
        public void WhenTheRowsMove_TheBandsMoveWithThem()
        {
            var (_, stripes, rows) = Show(rowsTop: 10);

            rows.Margin = new Thickness(0, -7, 0, 0);
            Dispatcher.UIThread.RunJobs();

            // The rows scrolled 17 up. The band of the first row is now at -7.
            var bands = stripes.Bands();
            Assert.Contains(bands, band => band.Band.Top == -7 && ReferenceEquals(band.Brush, First));
        }

        [AvaloniaFact]
        public void WithoutRows_TheBandsStartAtTheTop()
        {
            var (_, stripes, _) = Show(withRows: false);

            var bands = stripes.Bands();

            Assert.Equal(0, bands[0].Band.Top);
            Assert.Same(First, bands[0].Brush);
        }

        [AvaloniaFact]
        public void TheStripes_TakeNoPointerInput()
        {
            var (_, stripes, _) = Show();

            Assert.False(stripes.IsHitTestVisible);
        }
    }
}
