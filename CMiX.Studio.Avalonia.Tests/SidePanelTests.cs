using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // Each side of the window has a combo box at its top. The user picks what the side shows: the tree or the tabs, on either
    // side, or the same on both sides.
    public class SidePanelTests
    {
        private sealed class Screen
        {
            public MainWindow Window { get; init; } = null!;
            public MainViewModel ViewModel { get; init; } = null!;

            public List<SidePanelHost> Hosts => Window.GetVisualDescendants().OfType<SidePanelHost>().OrderBy(host => host.TranslatePoint(new Point(0, 0), Window)!.Value.X).ToList();
            public SidePanelHost Left => Hosts[0];
            public SidePanelHost Right => Hosts[1];
            public List<ProjectTreeView> Trees => Window.GetVisualDescendants().OfType<ProjectTreeView>().ToList();
            public List<MainTabsPanel> Tabs => Window.GetVisualDescendants().OfType<MainTabsPanel>().ToList();

            public static ComboBox PickerOf(SidePanelHost host) => host.GetVisualDescendants().OfType<ComboBox>().First(combo => combo.Name == "picker");
        }

        private static Screen Show()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.SelectedItem;
            composition.LayerManager.AddItem(typeof(Layer));
            TestServiceProviderFactory.Pump();
            return new Screen { Window = window, ViewModel = viewModel };
        }

        private static void Pick(Screen screen, bool left, PanelChoice choice)
        {
            var host = left ? screen.Left : screen.Right;
            Screen.PickerOf(host).SelectedItem = choice;
            TestServiceProviderFactory.Pump();
        }

        [AvaloniaFact]
        public void ByDefault_TheTreeIsOnTheLeft_AndTheTabsOnTheRight()
        {
            var screen = Show();

            Assert.Single(screen.Trees);
            Assert.Single(screen.Tabs);
            Assert.Same(PanelChoice.Tree, screen.ViewModel.LeftPanel);
            Assert.Same(PanelChoice.Tabs, screen.ViewModel.RightPanel);
            Assert.True(screen.Trees[0].TranslatePoint(new Point(0, 0), screen.Window)!.Value.X < screen.Tabs[0].TranslatePoint(new Point(0, 0), screen.Window)!.Value.X);
        }

        [AvaloniaFact]
        public void EachSide_HasAComboBoxAtItsTop_WithTheTwoChoices_AndTheCurrentOneSelected()
        {
            var screen = Show();

            foreach (var host in screen.Hosts)
            {
                var picker = Screen.PickerOf(host);
                Assert.Equal(new[] { "Tree", "Tabs" }, picker.Items.Cast<PanelChoice>().Select(choice => choice.Name));
                var hostTop = host.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y;
                var pickerTop = picker.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y;
                Assert.True(pickerTop - hostTop < 10, "The combo box is not at the top of the panel.");
            }

            Assert.Same(PanelChoice.Tree, Screen.PickerOf(screen.Left).SelectedItem);
            Assert.Same(PanelChoice.Tabs, Screen.PickerOf(screen.Right).SelectedItem);
        }

        [AvaloniaFact]
        public void TheTree_CanBeOnTheRight_WhileTheTabsAreOnTheLeft()
        {
            var screen = Show();

            Pick(screen, left: true, PanelChoice.Tabs);
            Pick(screen, left: false, PanelChoice.Tree);

            Assert.Same(PanelChoice.Tabs, screen.ViewModel.LeftPanel);
            Assert.Same(PanelChoice.Tree, screen.ViewModel.RightPanel);
            var tree = Assert.Single(screen.Trees);
            var tabs = Assert.Single(screen.Tabs);
            Assert.True(tabs.TranslatePoint(new Point(0, 0), screen.Window)!.Value.X < tree.TranslatePoint(new Point(0, 0), screen.Window)!.Value.X);
            Assert.Same(screen.ViewModel.ProjectTree, tree.DataContext);
            Assert.Same(screen.ViewModel, tabs.DataContext);
        }

        [AvaloniaFact]
        public void TheTree_CanBeOnBothSides_AndTheSelectionIsTheSame()
        {
            var screen = Show();

            Pick(screen, left: false, PanelChoice.Tree);

            Assert.Equal(2, screen.Trees.Count);
            Assert.Empty(screen.Tabs);
            Assert.All(screen.Trees, tree => Assert.Same(screen.ViewModel.ProjectTree, tree.DataContext));

            screen.ViewModel.ProjectTree.SelectedNode = screen.ViewModel.ProjectTree.Roots[0].Children[0];
            TestServiceProviderFactory.Pump();

            Assert.All(screen.Trees, tree =>
            {
                var selected = tree.GetVisualDescendants().OfType<TreeViewItem>().Single(item => item.IsSelected);
                Assert.Same(screen.ViewModel.ProjectTree.SelectedNode, selected.DataContext);
            });
        }

        [AvaloniaFact]
        public void TheTabs_CanBeOnBothSides()
        {
            var screen = Show();

            Pick(screen, left: true, PanelChoice.Tabs);

            Assert.Equal(2, screen.Tabs.Count);
            Assert.Empty(screen.Trees);
        }

        [AvaloniaFact]
        public void TheMasterBeat_IsPartOfTheTabsPanel_WhereverItIs()
        {
            var screen = Show();
            Pick(screen, left: true, PanelChoice.Tabs);
            Pick(screen, left: false, PanelChoice.Tree);

            var beat = screen.Window.GetVisualDescendants().OfType<MasterBeatControl>().Single();
            var tabs = Assert.Single(screen.Tabs);

            Assert.True(tabs.IsVisualAncestorOf(beat));
        }

        [AvaloniaFact]
        public void TheViewModel_ChangesTheComboBox_AndTheOtherWayAround()
        {
            var screen = Show();

            screen.ViewModel.RightPanel = PanelChoice.Tree;
            TestServiceProviderFactory.Pump();
            Assert.Same(PanelChoice.Tree, Screen.PickerOf(screen.Right).SelectedItem);
            Assert.Equal(2, screen.Trees.Count);

            Pick(screen, left: false, PanelChoice.Tabs);
            Assert.Same(PanelChoice.Tabs, screen.ViewModel.RightPanel);
            Assert.Single(screen.Tabs);
        }

        [AvaloniaFact]
        public void AClickOnATree_ShowsTheInspectorOfTheTabsOnTheOtherSide()
        {
            var screen = Show();
            Pick(screen, left: true, PanelChoice.Tabs);
            Pick(screen, left: false, PanelChoice.Tree);
            var tabs = Assert.Single(screen.Tabs);
            TestServiceProviderFactory.SelectMainTab(screen.Window, "Settings");

            var tree = Assert.Single(screen.Trees);
            var layerNode = screen.ViewModel.ProjectTree.Roots[0].Children[0];
            var item = tree.GetVisualDescendants().OfType<TreeViewItem>().First(row => ReferenceEquals(row.DataContext, layerNode));
            var row = item.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            screen.Window.UpdateLayout();
            var point = row.TranslatePoint(new Point(row.Bounds.Width * 0.5, row.Bounds.Height / 2), screen.Window)!.Value;
            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            TestServiceProviderFactory.Pump();

            var selected = (tabs.GetVisualDescendants().OfType<TabControl>().First().SelectedItem as TabItem)?.Header as AppIcon;
            Assert.Equal("Inspector", selected?.IconKey);
        }

        [AvaloniaFact]
        public void AClickOnATree_WithNoTabsOnScreen_ChangesNothingElse()
        {
            var screen = Show();
            Pick(screen, left: false, PanelChoice.Tree);
            var layerNode = screen.ViewModel.ProjectTree.Roots[0].Children[0];
            var tree = screen.Trees[1];
            var item = tree.GetVisualDescendants().OfType<TreeViewItem>().First(row => ReferenceEquals(row.DataContext, layerNode));
            var row = item.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            screen.Window.UpdateLayout();
            var point = row.TranslatePoint(new Point(row.Bounds.Width * 0.5, row.Bounds.Height / 2), screen.Window)!.Value;

            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            TestServiceProviderFactory.Pump();

            Assert.Same(layerNode, screen.ViewModel.ProjectTree.SelectedNode);
        }

        [AvaloniaFact]
        public void TheTwoSides_HaveTheirContentAtTheSameHeight()
        {
            var screen = Show();
            var tree = Assert.Single(screen.Trees);
            var tabs = TestServiceProviderFactory.MainTabControl(screen.Window);

            Assert.Equal(tree.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y, tabs.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y, 0.5);
        }

        [AvaloniaFact]
        public void TheHost_IsReusable_AnyPanelOfTheWindowCanUseIt()
        {
            var host = new SidePanelHost { Choices = PanelChoice.All, Choice = PanelChoice.Tabs };

            Assert.Same(PanelChoice.Tabs, host.Choice);
            Assert.Equal(2, host.Choices!.Cast<object>().Count());
            Assert.Equal(typeof(PanelChoice), PanelChoice.All[0].GetType().BaseType);
        }
    }
}
