using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.Boolean;
using CMiX.UI.Popups;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using Camera = CMiX.Core.Rendering.Cameras.Camera;
using Entity = CMiX.Core.Compositing.Entity;

namespace CMiX.Studio.Avalonia.Tests
{
    // The + at the right of a row: when it shows, where it is, what a press on it does not do, and the panel that it opens.
    public class ProjectTreeAddButtonTests : IDisposable
    {
        private readonly List<(Window Window, ProjectTreeView View)> shown = new();

        public void Dispose()
        {
            // A test without a window runs on another thread and has nothing to close.
            if (shown.Count == 0)
                return;

            foreach (var (window, view) in shown)
            {
                view.RowAddPanel.Close();
                window.Close();
            }

            Dispatcher.UIThread.RunJobs();
        }

        private sealed record Screen(Window Window, ProjectTreeView View, Scene Scene)
        {
            public TreeViewItem RowOf(TreeNode node) =>
                View.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

            public ButtonIcon PlusOf(TreeNode node) =>
                View.GetVisualDescendants().OfType<ButtonIcon>().First(button => button.Name == "AddRowButton" && ReferenceEquals(button.DataContext, node));

            // The colored row of a node, the part that takes the pointer.
            public Border BorderOf(TreeNode node) =>
                RowOf(node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");

            public Rect ToggleColumnOf(TreeNode node) =>
                new(RowOf(node).GetVisualDescendants().OfType<RowToggles>().First(toggles => ReferenceEquals(toggles.DataContext, node))
                        .TranslatePoint(default, Window)!.Value, new Size(1, 1));

            public Point CenterOf(Control control) =>
                control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)!.Value;

            public void Settle()
            {
                Window.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }

            public void Hover(Control control)
            {
                Settle();
                Window.MouseMove(CenterOf(control));
                Dispatcher.UIThread.RunJobs();
            }

            public void Click(Control control)
            {
                Hover(control);
                var point = CenterOf(control);
                Window.MouseDown(point, MouseButton.Left);
                Window.MouseUp(point, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
            }
        }

        // A composition with two layers. The first layer holds an entity and a camera.
        private Screen Show()
        {
            var scene = TreeFixtures.CreateScene([2]);
            scene.Layer.ModelEntityManager.AddItem(typeof(Entity));
            scene.Layer.CameraManager.AddItem(typeof(Camera));
            var view = new ProjectTreeView { DataContext = scene.Tree, Width = 400, HorizontalAlignment = HorizontalAlignment.Left };
            var window = new Window { Content = view, Width = 900, Height = 700 };
            window.Show();
            shown.Add((window, view));
            var screen = new Screen(window, view, scene);
            screen.Settle();
            return screen;
        }

        [AvaloniaFact]
        public void Show_ARowThatIsNeitherHoveredNorSelected_HasNoVisiblePlus()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = null;
            screen.Settle();

            Assert.False(screen.PlusOf(screen.Scene.LayerNodeOfA(1)).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Select_ARow_ShowsItsPlusSolid()
        {
            var screen = Show();

            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(1);
            screen.Settle();

            var plus = screen.PlusOf(screen.Scene.LayerNodeOfA(1));
            Assert.True(plus.IsEffectivelyVisible);
            Assert.Equal(1, plus.Opacity);
            Assert.False(screen.PlusOf(screen.Scene.LayerNodeOfA(0)).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Hover_ARow_ShowsItsPlusSoftAndNotThePlusOfItsParent()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = null;
            var child = screen.Scene.LayerNodeOfA(1);

            screen.Hover(screen.BorderOf(child));

            var plus = screen.PlusOf(child);
            Assert.True(plus.IsEffectivelyVisible);
            Assert.True(plus.Opacity < 1);
            Assert.False(screen.PlusOf(screen.Scene.CompositionNode).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Show_AGroupRowThatIsNeitherHoveredNorSelected_HasNoVisiblePlus()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = null;
            var group = Scene.Group<EntitiesGroup>(screen.Scene.LayerNode);

            screen.Settle();

            Assert.False(screen.PlusOf(group).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Hover_AGroupRow_ShowsItsPlusLikeAnyOtherRow()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = null;
            var group = Scene.Group<EntitiesGroup>(screen.Scene.LayerNode);

            screen.Hover(screen.BorderOf(group));

            Assert.True(screen.PlusOf(group).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Select_AGroupRow_ShowsItsPlus()
        {
            var screen = Show();
            var group = Scene.Group<EntitiesGroup>(screen.Scene.LayerNode);

            screen.Scene.Tree.SelectedNode = group;
            screen.Settle();

            Assert.True(screen.PlusOf(group).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Show_RowsWithAndWithoutAVisiblePlus_KeepTheirTogglesInTheSameColumn()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(0);
            screen.Settle();

            var withPlus = screen.ToggleColumnOf(screen.Scene.LayerNodeOfA(0));
            var without = screen.ToggleColumnOf(screen.Scene.LayerNodeOfA(1));

            Assert.True(screen.PlusOf(screen.Scene.LayerNodeOfA(0)).IsEffectivelyVisible);
            Assert.False(screen.PlusOf(screen.Scene.LayerNodeOfA(1)).IsEffectivelyVisible);
            Assert.Equal(withPlus.X, without.X, 0.51);
        }

        [AvaloniaFact]
        public void Show_ThePlus_IsAtTheFarRightOfTheRowAfterTheToggles()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(0);
            screen.Settle();

            var plus = screen.PlusOf(screen.Scene.LayerNodeOfA(0));
            var plusBox = plus.TranslatePoint(default, screen.Window)!.Value;
            var toggles = screen.ToggleColumnOf(screen.Scene.LayerNodeOfA(0));
            var rowRight = screen.RowOf(screen.Scene.LayerNodeOfA(0)).TranslatePoint(new Point(screen.RowOf(screen.Scene.LayerNodeOfA(0)).Bounds.Width, 0), screen.Window)!.Value.X;
            var height = Convert.ToDouble(Application.Current!.FindResource("ControlHeightMD"));

            Assert.True(plusBox.X > toggles.X);
            Assert.Equal(rowRight, plusBox.X + plus.Bounds.Width, 0.51);
            Assert.Equal(height, plus.Bounds.Height, 0.51);
        }

        [AvaloniaFact]
        public void Click_ThePlusOfARow_OpensThePanelOnItsRightAndKeepsTheSelection()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(1);
            var node = screen.Scene.LayerNodeOfA(0);
            screen.Hover(screen.BorderOf(node));
            var plus = screen.PlusOf(node);

            screen.Click(plus);
            screen.Settle();

            var panel = screen.View.RowAddPanel;
            var origin = plus.TranslatePoint(default, screen.Window)!.Value;
            var box = panel.Card.GetVisualDescendants().OfType<Border>().First(border => border.Name == "PART_Card");
            var search = panel.List.GetVisualDescendants().OfType<TextBox>().Single(text => text.Name == "PART_Search");
            Assert.True(panel.IsOpen);
            Assert.Equal(new[] { "Content", "Filter", "Modifier", "Project" }, panel.List.Groups!.Select(group => group.Header));
            Assert.Equal(origin.X + plus.Bounds.Width + 8, box.TranslatePoint(default, screen.Window)!.Value.X, 0.51);
            Assert.Equal(origin.Y + plus.Bounds.Height / 2, search.TranslatePoint(new Point(0, search.Bounds.Height / 2), screen.Window)!.Value.Y, 0.51);
            Assert.Same(screen.Scene.LayerNodeOfA(1), screen.Scene.Tree.SelectedNode);
        }

        [AvaloniaFact]
        public void Pick_ARowOfThePanel_AddsUnderTheRowOfThePlus()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(1);
            var node = screen.Scene.LayerNodeOfA(0);
            screen.Hover(screen.BorderOf(node));
            screen.Click(screen.PlusOf(node));
            screen.Settle();

            var panel = screen.View.RowAddPanel;
            var texts = panel.List.VisibleTexts;
            var light = panel.List.RowVisuals[texts.ToList().IndexOf("Light")];
            screen.Hover(light);
            screen.Click(light);

            Assert.False(panel.IsOpen);
            Assert.Single(screen.Scene.LayerOfA(0).LightManager.ManagerData.Items);
            Assert.Empty(screen.Scene.LayerOfA(1).LightManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void Press_ThePlusAndMove_StartsNoDragAndChoosesNoNode()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = screen.Scene.LayerNodeOfA(0);
            var node = screen.Scene.LayerNodeOfA(0);
            var chosen = 0;
            screen.View.NodeChosen += (_, _) => chosen++;
            var plus = screen.PlusOf(node);
            screen.Hover(plus);

            var start = screen.CenterOf(plus);
            screen.Window.MouseDown(start, MouseButton.Left);
            screen.Window.MouseMove(new Point(start.X, start.Y + 60));
            screen.Window.MouseUp(new Point(start.X, start.Y + 60), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.False(screen.View.RowDrag.IsDragging);
            Assert.Equal(0, chosen);
            Assert.Equal(2, screen.Scene.A.LayerManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void Click_ThePlusOfAnUnselectedRow_DoesNotSelectIt()
        {
            var screen = Show();
            screen.Scene.Tree.SelectedNode = null;
            var node = screen.Scene.LayerNodeOfA(1);
            screen.Hover(screen.BorderOf(node));

            screen.Click(screen.PlusOf(node));

            Assert.Null(screen.Scene.Tree.SelectedNode);
        }
    }
}
