using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using static CMiX.Studio.Avalonia.Tests.Support.TreeTestScreen;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tooltips of the rows of the tree open and close by the tree itself, with the show delay of the control.
    public class TreeTooltipsTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        private (ProjectTreeView View, ProjectTree Tree) Show()
        {
            var scene = CreateScene([1], s => AddKinds(s.Layer));
            var screen = TreeTestScreen.Show(scene.Tree, 500, 600);
            window = screen.Window;
            return (screen.View, scene.Tree);
        }

        private static ToggleButton MaskOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<ToggleButton>().First(toggle => Equals(ToolTip.GetTip(toggle), "Mask") && toggle.IsEffectivelyVisible);

        [AvaloniaFact]
        public void Create_TheTree_SwitchesTheTooltipServiceOffForItsRows()
        {
            var (view, _) = Show();
            var rows = view.GetVisualDescendants().OfType<TreeView>().Single();

            Assert.False(ToolTip.GetServiceEnabled(rows));
            Assert.False(ToolTip.GetServiceEnabled(MaskOf(view, view.DataContext is ProjectTree tree ? tree.CompositionNodes()[0] : null!)));
        }

        [AvaloniaFact]
        public void Hover_AControlWithATip_OpensItsTooltipAfterTheDelay()
        {
            var (view, tree) = Show();
            var mask = MaskOf(view, tree.CompositionNodes()[0]);
            ToolTip.SetShowDelay(mask, 0);

            view.Tooltips.Hover(mask);

            Assert.True(ToolTip.GetIsOpen(mask));
            Assert.Same(mask, view.Tooltips.Open);
        }

        [AvaloniaFact]
        public void Hover_AControlWithADelay_WaitsForTheTimerBeforeItOpens()
        {
            var (view, tree) = Show();
            var mask = MaskOf(view, tree.CompositionNodes()[0]);

            view.Tooltips.Hover(mask);
            Assert.False(ToolTip.GetIsOpen(mask));
            Assert.Same(mask, view.Tooltips.Hovered);

            view.Tooltips.Show();
            Assert.True(ToolTip.GetIsOpen(mask));
        }

        [AvaloniaFact]
        public void Hover_AnotherControlOrNone_ClosesTheOpenTooltip()
        {
            var (view, tree) = Show();
            var first = MaskOf(view, tree.CompositionNodes()[0]);
            var second = ItemOf(view, tree.CompositionNodes()[0].Children[0]).GetVisualDescendants().OfType<ToggleButton>().First(toggle => toggle != first && ToolTip.GetTip(toggle) != null && toggle.IsEffectivelyVisible);
            ToolTip.SetShowDelay(first, 0);
            ToolTip.SetShowDelay(second, 0);
            view.Tooltips.Hover(first);

            view.Tooltips.Hover(second);
            Assert.False(ToolTip.GetIsOpen(first));
            Assert.True(ToolTip.GetIsOpen(second));

            view.Tooltips.Hover(null);
            Assert.False(ToolTip.GetIsOpen(second));
            Assert.Null(view.Tooltips.Open);
        }

        [AvaloniaFact]
        public void HostOf_AChildOfAControlWithATip_IsThatControl()
        {
            var (view, tree) = Show();
            var mask = MaskOf(view, tree.CompositionNodes()[0]);
            var inside = mask.GetVisualDescendants().OfType<Visual>().Last();

            Assert.Same(mask, TreeTooltips.HostOf(inside));
        }

        [AvaloniaFact]
        public void HostOf_ADisabledControl_IsSkippedUnlessItShowsOnDisabled()
        {
            var (view, tree) = Show();
            var mask = MaskOf(view, tree.CompositionNodes()[0]);
            mask.IsEnabled = false;
            var parentHost = TreeTooltips.HostOf(mask);

            Assert.NotSame(mask, parentHost);

            ToolTip.SetShowOnDisabled(mask, true);
            Assert.Same(mask, TreeTooltips.HostOf(mask));
        }
    }
}
