using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls.String;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The first and the last row, and the first column, of the pixels that the white text covers, in the display mode and in the edit mode.
    // This needs the real renderer, which TestAppBuilder starts, because only it has the real metrics of the font.
    // The border and the background of the input are grey, so only the text is counted.
    public class NameBoxPixelTests
    {
        private static (int Top, int Bottom, int Left) InkRows(Window window, Rect region)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No rendered frame: the test needs the real renderer.");
            using var locked = frame.Lock();
            var top = int.MaxValue;
            var bottom = int.MinValue;
            var left = int.MaxValue;
            for (var y = (int)region.Top; y < (int)region.Bottom; y++)
            {
                for (var x = (int)region.Left; x < (int)region.Right; x++)
                {
                    var pixel = System.Runtime.InteropServices.Marshal.ReadInt32(locked.Address, y * locked.RowBytes + x * 4);
                    var b = pixel & 0xFF;
                    var g = (pixel >> 8) & 0xFF;
                    var r = (pixel >> 16) & 0xFF;
                    if (r + g + b > 3 * 150)
                    {
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                        left = Math.Min(left, x);
                    }
                }
            }

            return (top, bottom, left);
        }

        // The ink rows of one box in a fresh window, in the display mode or in the edit mode.
        private static (int Top, int Bottom, int Left) Ink(double fontSize, bool editing)
        {
            var box = new TextBoxEditable
            {
                Text = "HEg Layer",
                FontSize = fontSize,
                Foreground = Brushes.White,
                Height = 24,
                Margin = new Thickness(10, 10, 0, 0),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
                Width = 200,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
            };
            var window = new Window { Content = box, Width = 300, Height = 100, Background = new SolidColorBrush(Color.Parse("#FF111113")) };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            if (editing)
            {
                box.IsEditing = true;
                Dispatcher.UIThread.RunJobs();
                var input = box.GetVisualDescendants().OfType<TextBox>().First();
                input.CaretBrush = Brushes.Transparent;
                input.SelectionBrush = Brushes.Transparent;
                input.Foreground = Brushes.White;
                input.ClearSelection();
            }

            window.UpdateLayout();
            var origin = box.TranslatePoint(new Point(0, 0), window)!.Value;
            var ink = InkRows(window, new Rect(origin.X + 6, origin.Y, 150, 24));
            window.Close();
            return ink;
        }

        [AvaloniaTheory]
        [InlineData(12.0)]
        [InlineData(12.5)]
        [InlineData(13.0)]
        [InlineData(11.0)]
        [InlineData(10.0)]
        public void Render_TheText_PutsTheInkAtTheSamePixelsInBothModes(double fontSize)
        {
            var shown = Ink(fontSize, editing: false);
            var edited = Ink(fontSize, editing: true);

            Assert.True(shown == edited, $"font {fontSize}: shown {shown} edited {edited}");
        }
    }
}
