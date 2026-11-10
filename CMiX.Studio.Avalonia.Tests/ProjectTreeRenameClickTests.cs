using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.UI.Controls.String;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;

namespace CMiX.Studio.Avalonia.Tests
{
    // A right click while a row of the tree is renamed cancels the rename, and does nothing else: no menu opens.
    public class ProjectTreeRenameClickTests : IDisposable
    {
        private TreeTestScreen? screen;

        public void Dispose()
        {
            screen?.View.GetVisualDescendants().OfType<TreeView>().FirstOrDefault()?.ContextMenu?.Close();
            screen?.Dispose();
        }

        private static Point Middle(Window window, Control control) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

        private static void RightClick(Window window, Point point)
        {
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Right);
            window.MouseUp(point, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaFact]
        public void RightClick_OnAnotherRowDuringARename_CancelsTheRenameAndOpensNoMenu()
        {
            var scene = CreateScene([2]);
            screen = TreeTestScreen.Show(scene.Tree, 400, 700);
            var renamed = scene.LayerNodeOfA(0);
            var other = scene.LayerNodeOfA(1);
            var name = renamed.Header;
            scene.Tree.SelectedNode = renamed;
            screen.Settle();
            scene.Tree.RenameSelectedCommand.Execute(null);
            screen.Settle();
            var box = TreeTestScreen.RowOf(screen.View, renamed).GetVisualDescendants().OfType<TextBoxEditable>().First();
            Assert.True(box.IsEditing);
            box.GetVisualDescendants().OfType<TextBox>().First(input => input.IsVisible).Text = "Typed";

            RightClick(screen.Window, Middle(screen.Window, TreeTestScreen.RowOf(screen.View, other)));

            var treeView = screen.View.GetVisualDescendants().OfType<TreeView>().First();
            Assert.False(box.IsEditing);
            Assert.Equal(name, renamed.Header);
            Assert.NotEqual(true, treeView.ContextMenu?.IsOpen);

            // The rename is over: the next right click opens the menu of the row.
            RightClick(screen.Window, Middle(screen.Window, TreeTestScreen.RowOf(screen.View, other)));
            Assert.True(treeView.ContextMenu?.IsOpen);
        }
    }
}
