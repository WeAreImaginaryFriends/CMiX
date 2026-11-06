using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls;
using CMiX.UI.Controls.Boolean;
using Xunit;

namespace CMiX.UI.Tests
{
    // ButtonIcon is a Button with one icon. Its Tip is its tooltip.
    public class ButtonIconTests
    {
        private static Window Show(Control content)
        {
            var window = new Window { Content = content, Width = 100, Height = 100 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return window;
        }

        [AvaloniaFact]
        public void IconKey_Set_ShowsTheIcon()
        {
            var button = new ButtonIcon { IconKey = "Add" };
            Show(button);

            var icon = button.GetVisualDescendants().OfType<AppIcon>().Single();
            Assert.Equal("Add", icon.IconKey);

            button.IconKey = "Remove";
            Assert.Equal("Remove", icon.IconKey);
        }

        [AvaloniaFact]
        public void Tip_WhenSet_GivesATooltip()
        {
            var button = new ButtonIcon { IconKey = "Add", Tip = "Add" };

            Assert.Equal("Add", ToolTip.GetTip(button));
        }

        [AvaloniaFact]
        public void Tip_WhenEmpty_GivesNoTooltip()
        {
            var button = new ButtonIcon { IconKey = "Add", Tip = "Add" };

            button.Tip = string.Empty;

            Assert.Null(ToolTip.GetTip(button));
        }
    }
}
