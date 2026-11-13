using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.UI.Controls.Parts;
using Xunit;

namespace CMiX.UI.Tests
{
    // The drop down of the main menu has the look of a context menu: rows of MenuItemContext in a PopupFrame.
    public class MainMenuDropdownLookTests
    {
        [AvaloniaFact]
        public void Open_ARowOfTheMenuBar_ShowsContextMenuRowsInAPopupFrame()
        {
            var open = new MenuItem { Header = "Open" };
            var file = new MenuItem
            {
                Header = "File",
                Theme = (ControlTheme)Application.Current!.FindResource("MenuItemTop")!,
                ItemsSource = new object[] { open },
            };
            var menu = new Menu { Theme = (ControlTheme)Application.Current!.FindResource("MenuDefault")!, ItemsSource = new object[] { file } };
            new Window { Content = menu, Width = 500, Height = 400 }.Show();
            Dispatcher.UIThread.RunJobs();

            file.Open();
            Dispatcher.UIThread.RunJobs();

            var popup = file.GetVisualDescendants().OfType<Popup>().Single(part => part.Name == "PART_Popup");
            Assert.IsType<PopupFrame>(popup.Child);
            Assert.Same(Application.Current!.FindResource("MenuItemContext"), open.Theme);
            Assert.Equal((double)Application.Current!.FindResource("ControlHeightMD")!, open.Bounds.Height);
        }
    }
}
