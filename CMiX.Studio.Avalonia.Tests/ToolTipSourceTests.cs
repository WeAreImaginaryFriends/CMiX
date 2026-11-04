using System.Text.RegularExpressions;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // Every tooltip of the app opens below its control, 8 pixels away (see ToolTipDefaults). A view sets the tip only.
    public class ToolTipSourceTests
    {
        private static readonly Regex Placement = new(@"ToolTip\.(Placement|HorizontalOffset|VerticalOffset)\s*=", RegexOptions.Compiled);

        [Fact]
        public void NoView_SetsThePlaceOfATooltip()
        {
            var found = SourceTree.AxamlFiles()
                .Where(file => Placement.IsMatch(File.ReadAllText(file)))
                .Select(Path.GetFileName)
                .ToList();

            Assert.True(found.Count == 0, "These views set the place of a tooltip: " + string.Join(", ", found));
        }
    }
}
