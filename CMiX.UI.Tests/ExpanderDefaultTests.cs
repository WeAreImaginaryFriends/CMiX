using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CMiX.UI.Panels;
using Xunit;

namespace CMiX.UI.Tests
{
    // A panel is open when it appears.
    public class ExpanderDefaultTests
    {
        private sealed class Flag
        {
            public bool Open { get; set; }
        }

        [AvaloniaFact]
        public void Show_AnIndentedExpander_IsOpenByDefault()
        {
            Assert.True(new ExpanderIndented().IsExpanded);
        }

        [AvaloniaFact]
        public void Show_AnExpanderInTheTheme_IsOpenByDefault()
        {
            var expander = new Expander { Header = "Peers", Content = new Border() };
            var window = new Window { Content = expander, Width = 200, Height = 200 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(expander.IsExpanded);
        }

        [AvaloniaFact]
        public void Show_AValueOrABinding_CanCloseAnExpander()
        {
            var closed = new ExpanderIndented { IsExpanded = false };
            var bound = new ExpanderIndented { DataContext = new Flag { Open = false } };
            bound.Bind(Expander.IsExpandedProperty, new Binding("Open"));

            Assert.False(closed.IsExpanded);
            Assert.False(bound.IsExpanded);
        }
    }
}
