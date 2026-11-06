using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI;
using CMiX.UI.Controls.Parts;
using Xunit;

namespace CMiX.UI.Tests
{
    // A tooltip opens below its control, with 8 pixels between the control and the box of the tooltip.
    public class ToolTipDefaultsTests
    {
        [AvaloniaFact]
        public void Placement_ByDefault_IsBelowEveryControl()
        {
            foreach (var control in new Control[] { new Button(), new TextBlock(), new Border(), new ToggleButton(), new CMiX.UI.Controls.AppIcon() })
            {
                ToolTip.SetTip(control, "A tip");
                Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(control));
                Assert.Equal(ToolTipDefaults.Gap - PopupFrame.ShadowMargin.Top, ToolTip.GetVerticalOffset(control));
                Assert.Equal(0, ToolTip.GetHorizontalOffset(control));
            }
        }

        [AvaloniaFact]
        public void Placement_SetForOneControl_StaysAsSet()
        {
            var button = new Button();
            ToolTip.SetPlacement(button, PlacementMode.Right);
            ToolTip.SetVerticalOffset(button, 3);

            ToolTip.SetTip(button, "A tip");

            Assert.Equal(PlacementMode.Right, ToolTip.GetPlacement(button));
            Assert.Equal(3, ToolTip.GetVerticalOffset(button));
        }

        [AvaloniaFact]
        public void Placement_ATipFromABinding_IsSetToo()
        {
            var button = new Button { DataContext = new { Text = "From a binding" } };
            button.Bind(ToolTip.TipProperty, new Avalonia.Data.Binding("Text"));

            Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(button));
        }

        [AvaloniaFact]
        public void Show_TheTooltipTheme_HasTheShadowRoomOfTheOtherPopups()
        {
            var tip = new ToolTip { Content = "Text" };
            var window = new Window { Content = new Border { Child = tip }, Width = 200, Height = 100 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            tip.ApplyTemplate();

            var frame = tip.GetVisualDescendants().OfType<Border>().First();

            Assert.Equal(PopupFrame.ShadowMargin, frame.Margin);
        }

        [AvaloniaFact]
        public void Open_ATooltip_IsEightPixelsBelowTheControl()
        {
            var button = new Button { Content = "Hover", Width = 80, Height = 24, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Margin = new Thickness(40, 20, 0, 0) };
            ToolTip.SetTip(button, "A tip");
            var window = new Window { Content = button, Width = 300, Height = 200 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            ToolTip.SetIsOpen(button, true);
            Dispatcher.UIThread.RunJobs();

            var tip = window.GetVisualDescendants().OfType<ToolTip>().FirstOrDefault()
                ?? (button.GetValue(ToolTip.TipProperty) as ToolTip);
            Assert.NotNull(tip);
            var box = tip!.GetVisualDescendants().OfType<Border>().First();
            var inner = box.Child as Border ?? box;
            var buttonBottom = button.TranslatePoint(new Point(0, button.Bounds.Height), window)!.Value.Y;
            var boxTop = box.TranslatePoint(new Point(0, 0), window)!.Value.Y;
            Assert.Equal(buttonBottom + ToolTipDefaults.Gap, boxTop, 1);
        }
    }
}
