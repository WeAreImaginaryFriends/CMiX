using System.Globalization;
using Avalonia.Input;
using CMiX.UI.Converters;
using Xunit;

namespace CMiX.UI.Tests
{
    // The gesture text: a number key shows as its digit, and a written digit reads as the number key.
    public class KeyGestureTextConverterTests
    {
        [Theory]
        [InlineData("Ctrl+1", Key.D1, KeyModifiers.Control)]
        [InlineData("Ctrl+3", Key.D3, KeyModifiers.Control)]
        [InlineData("Ctrl+Shift+1", Key.D1, KeyModifiers.Control | KeyModifiers.Shift)]
        [InlineData("Ctrl + 0", Key.D0, KeyModifiers.Control)]
        [InlineData("9", Key.D9, KeyModifiers.None)]
        [InlineData("Ctrl+D1", Key.D1, KeyModifiers.Control)]
        [InlineData("Shift+A", Key.A, KeyModifiers.Shift)]
        [InlineData("F2", Key.F2, KeyModifiers.None)]
        public void Parse_AText_GivesTheGesture(string text, Key key, KeyModifiers modifiers)
        {
            Assert.Equal(new KeyGesture(key, modifiers), KeyGestureTextConverter.Parse(text));
        }

        [Fact]
        public void Parse_ThePlusKey_KeepsTheAvaloniaReading()
        {
            Assert.Equal(KeyGesture.Parse("Ctrl++"), KeyGestureTextConverter.Parse("Ctrl++"));
        }

        [Fact]
        public void Convert_ANumberKey_ShowsTheDigit()
        {
            var converter = new KeyGestureTextConverter();

            var text = converter.Convert(new KeyGesture(Key.D1, KeyModifiers.Control), typeof(string), null, CultureInfo.InvariantCulture);

            Assert.Equal("Ctrl+1", text);
        }

        [Fact]
        public void TextOf_OtherKeys_GivesTheAvaloniaText()
        {
            Assert.Equal("Shift+D", KeyGestureTextConverter.TextOf(new KeyGesture(Key.D, KeyModifiers.Shift)));
            Assert.Equal("Ctrl+Shift+Z", KeyGestureTextConverter.TextOf(new KeyGesture(Key.Z, KeyModifiers.Control | KeyModifiers.Shift)));
        }

        [Fact]
        public void TextOf_ThenParse_GivesTheSameGesture()
        {
            var gesture = new KeyGesture(Key.D4, KeyModifiers.Control | KeyModifiers.Alt);

            Assert.Equal(gesture, KeyGestureTextConverter.Parse(KeyGestureTextConverter.TextOf(gesture)));
        }
    }
}
