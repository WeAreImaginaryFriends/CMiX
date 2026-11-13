using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using static CMiX.Studio.Avalonia.Tests.Support.TreeTestScreen;
using AvaloniaColor = Avalonia.Media.Color;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // A collapsed row shows what it holds: a square for each kind, in the color of the kind, with the number.
    public class ProjectTreeKindBadgesTests : IDisposable
    {
        private Window? window;
        private readonly CategoryPalette palette = CategoryPalette.FromApplication();

        public void Dispose() => window?.Close();

        // A composition with two layers. The first layer has two entities, one filter and three modifiers.
        private (ProjectTreeView View, ProjectTree Tree, TreeNode Layer, TreeNode Empty) Show()
        {
            var tree = CreateScene([2], s => AddKinds(s.Layer)).Tree;
            var screen = TreeTestScreen.Show(tree, 500, 600);
            window = screen.Window;
            var view = screen.View;
            tree.SelectedNode = null;
            Settle();
            return (view, tree, tree.CompositionNodes()[0].Children[0].Children[0], tree.CompositionNodes()[0].Children[0].Children[1]);
        }

        private void Settle() => TreeTestScreen.Settle(window!);

        private static KindCountBadges BadgesOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<KindCountBadges>().First(badges => ReferenceEquals(badges.DataContext, node));

        private static List<Border> SquaresOf(KindCountBadges badges) =>
            badges.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("kindcount")).ToList();


        private static string NumberIn(Border square) => square.GetVisualDescendants().OfType<TextBlock>().Single().Text!;

        [AvaloniaFact]
        public void Show_ACollapsedRow_ShowsASquareWithTheCountForEachKind()
        {
            var (view, _, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();

            var badges = BadgesOf(view, layer);
            var squares = SquaresOf(badges);

            Assert.True(badges.IsEffectivelyVisible);
            Assert.Equal(new[] { "2", "1", "3" }, squares.Select(NumberIn));
            Assert.Equal(
                new[] { TreeCategory.Entity, TreeCategory.Filter, TreeCategory.Modifier }.Select(category => palette.For(category).Kind),
                squares.Select(FillOf));
        }

        [AvaloniaFact]
        public void Show_AnOpenOrEmptyRow_ShowsNoSquares()
        {
            var (view, _, layer, empty) = Show();

            Assert.False(BadgesOf(view, layer).IsEffectivelyVisible);

            empty.IsExpanded = false;
            Settle();
            Assert.False(BadgesOf(view, empty).IsEffectivelyVisible);
            Assert.Empty(SquaresOf(BadgesOf(view, empty)));
        }

        [AvaloniaFact]
        public void Expand_ARow_HidesOrShowsTheSquares()
        {
            var (view, _, layer, _) = Show();

            layer.IsExpanded = false;
            Settle();
            Assert.True(BadgesOf(view, layer).IsEffectivelyVisible);

            layer.IsExpanded = true;
            Settle();
            Assert.False(BadgesOf(view, layer).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Show_ACollapsedGroup_ShowsItsCountInTheColorOfItsKind()
        {
            var (view, _, layer, _) = Show();
            var entities = layer.Children.Single(child => child.Group is EntitiesGroup);
            entities.IsExpanded = false;
            Settle();

            var square = Assert.Single(SquaresOf(BadgesOf(view, entities)));

            Assert.Equal("2", NumberIn(square));
            Assert.Equal(palette.For(TreeCategory.Entity).Kind, FillOf(square));
        }

        [AvaloniaFact]
        public void Select_ARow_MakesTheSquaresBrighter()
        {
            var (view, tree, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();
            var badges = BadgesOf(view, layer);
            var tone = palette.For(TreeCategory.Entity);

            Assert.Equal(tone.Kind, FillOf(SquaresOf(badges).First()));

            badges.IsHovered = true;
            Assert.Equal(tone.KindSelected, FillOf(SquaresOf(badges).First()));

            tree.SelectedNode = layer;
            Settle();
            Assert.Equal(tone.KindSelected, FillOf(SquaresOf(badges).First()));
        }

        private static AvaloniaColor NumberColorOf(Border square) =>
            ((ISolidColorBrush)square.GetVisualDescendants().OfType<TextBlock>().Single().Foreground!).Color;

        private static AvaloniaColor ColorOf(string key) => ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

        // The number is white on the square at rest, and dark on the full color of a row that is under the pointer or selected.
        [AvaloniaFact]
        public void Select_ARow_MakesTheNumbersDark()
        {
            var (view, tree, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();
            var badges = BadgesOf(view, layer);

            Assert.All(SquaresOf(badges), square => Assert.Equal(ColorOf("ActiveForegroundBrush"), NumberColorOf(square)));

            badges.IsHovered = true;
            Settle();
            Assert.All(SquaresOf(badges), square => Assert.Equal(ColorOf("TreeBackgroundBrush"), NumberColorOf(square)));

            badges.IsHovered = false;
            tree.SelectedNode = layer;
            Settle();
            Assert.All(SquaresOf(badges), square => Assert.Equal(ColorOf("TreeBackgroundBrush"), NumberColorOf(square)));
        }

        // The dark number can be read on the full color of every kind: a contrast of 4.5:1 or more.
        [AvaloniaTheory]
        [InlineData(TreeCategory.Composable)]
        [InlineData(TreeCategory.Entity)]
        [InlineData(TreeCategory.Camera)]
        [InlineData(TreeCategory.Light)]
        [InlineData(TreeCategory.Texture)]
        [InlineData(TreeCategory.Filter)]
        [InlineData(TreeCategory.Modifier)]
        [InlineData(TreeCategory.Modulator)]
        public void TheDarkNumber_OnTheFullColorOfAKind_CanBeRead(TreeCategory category)
        {
            var dark = ColorOf("TreeBackgroundBrush");

            // A count square has the color of its kind at the level 0 (see TreeToneConverter).
            Assert.True(CMiX.UI.Mathematics.ColorMix.Contrast(dark, palette.For(category).KindSelected) >= 4.5);
        }

        [AvaloniaFact]
        public void Show_ASquare_IsSixteenHighInTheMiddleOfTheRow()
        {
            var (view, _, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();
            var row = ItemOf(view, layer).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
            var square = SquaresOf(BadgesOf(view, layer)).First();

            var top = square.TranslatePoint(new Point(0, 0), row)!.Value.Y;
            var bottom = row.Bounds.Height - (top + square.Bounds.Height);

            Assert.Equal(16, square.Bounds.Height);
            Assert.Equal(top, bottom, 1);
            Assert.True(top > 0);
        }

        private static Border MoreKindsOf(KindCountBadges badges) =>
            badges.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "MoreKinds");

        private static TextBlock HiddenCountOf(KindCountBadges badges) =>
            badges.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "HiddenCount");

        private static void Hide(TreeNode node) => ((CMiX.Core.Prefabs.IPrefab)node.Item!).PrefabService.Visibility.Value = true;

        [AvaloniaFact]
        public void Show_ARowWithFiveKinds_ShowsThreeSquaresAndPlusTwo()
        {
            var tree = CreateScene([1], s =>
            {
                AddKinds(s.Layer);
                s.Layer.CameraManager.AddItem(typeof(CMiX.Core.Rendering.Cameras.Camera));
                s.Layer.LightManager.AddItem(typeof(CMiX.Core.Rendering.Lights.LightEntity));
            }).Tree;
            var screen = TreeTestScreen.Show(tree, 500, 600);
            window = screen.Window;
            var layer = tree.CompositionNodes()[0].Children[0].Children[0];
            layer.IsExpanded = false;
            Settle();

            var badges = BadgesOf(screen.View, layer);
            var more = MoreKindsOf(badges);

            Assert.Equal(new[] { "2", "1", "3" }, SquaresOf(badges).Select(NumberIn));
            Assert.True(more.IsEffectivelyVisible);
            Assert.Equal("+2", NumberIn(more));
            Assert.Equal("1 Camera" + Environment.NewLine + "1 Light", ToolTip.GetTip(more));
        }

        [AvaloniaFact]
        public void Show_ARowWithThreeKinds_ShowsNoPlusSquare()
        {
            var (view, _, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();

            Assert.False(MoreKindsOf(BadgesOf(view, layer)).IsEffectivelyVisible);
            Assert.False(HiddenCountOf(BadgesOf(view, layer)).IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Hide_RowsUnderACollapsedRow_ShowsTheHiddenCountInTheDisabledLook()
        {
            var (view, tree, layer, _) = Show();
            var composition = tree.CompositionNodes()[0];
            var entities = layer.Children.Single(child => child.Group is EntitiesGroup);

            Hide(entities.Children[0]);
            Hide(entities.Children[1]);
            layer.IsExpanded = false;
            Settle();

            var text = HiddenCountOf(BadgesOf(view, layer));
            Assert.True(text.IsEffectivelyVisible);
            Assert.Equal("2 hidden", text.Text);
            Assert.Equal((double)Application.Current!.FindResource("OpacityDisabled")!, text.Opacity);

            // A row higher up counts every hidden row under it: the layer and its two entities.
            Hide(layer);
            composition.IsExpanded = false;
            Settle();
            Assert.Equal(3, composition.HiddenCount);
            Assert.Equal("3 hidden", HiddenCountOf(BadgesOf(view, composition)).Text);

            ((CMiX.Core.Prefabs.IPrefab)layer.Item!).PrefabService.Visibility.Value = false;
            Settle();
            Assert.Equal("2 hidden", HiddenCountOf(BadgesOf(view, composition)).Text);
        }

        [AvaloniaFact]
        public void Hide_AnEmptyHiddenGroup_ShowsTheCountOnTheCollapsedRow()
        {
            var (view, _, _, empty) = Show();
            var cameras = empty.Children.Single(child => child.Group is CamerasGroup);

            cameras.Group!.Manager.Hidden.Value = true;
            empty.IsExpanded = false;
            Settle();

            var badges = BadgesOf(view, empty);
            Assert.True(badges.IsEffectivelyVisible);
            Assert.Empty(SquaresOf(badges));
            Assert.Equal("1 hidden", HiddenCountOf(badges).Text);
        }

        [AvaloniaFact]
        public void Rename_ARow_MovesTheSquares()
        {
            var (view, _, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();
            var item = ItemOf(view, layer);
            var name = item.GetVisualDescendants().OfType<TextBlock>().First(text => text.Name == "TextDisplay");
            var badges = BadgesOf(view, layer);

            var nameRight = name.TranslatePoint(new Point(name.Bounds.Width, 0), window!)!.Value.X;
            var badgesLeft = badges.TranslatePoint(new Point(0, 0), window!)!.Value.X;

            Assert.InRange(badgesLeft - nameRight, 0, 12);
        }
    }
}
