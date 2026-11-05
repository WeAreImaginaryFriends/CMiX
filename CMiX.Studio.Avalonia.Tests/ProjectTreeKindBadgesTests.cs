using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Themes;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
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
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.ModelEntityManager.AddItem(typeof(Entity));
            layer.TextureModifierManager.AddItem(typeof(CMiX.Core.Texturing.Filters.Blur));
            for (var i = 0; i < 3; i++)
                layer.ModifierManager.AddItem(typeof(CMiX.Core.Modulation.Modifiers.TranslateModifier));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 600 };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            tree.SelectedNode = null;
            Settle();
            return (view, tree, tree.Roots[0].Children[0], tree.Roots[0].Children[1]);
        }

        private void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window!.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static KindCountBadges BadgesOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<KindCountBadges>().First(badges => ReferenceEquals(badges.DataContext, node));

        private static List<Border> SquaresOf(KindCountBadges badges) =>
            badges.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("kindcount")).ToList();

        private static AvaloniaColor FillOf(Border square) => ((ISolidColorBrush)square.Background!).Color;

        private static string NumberIn(Border square) => square.GetVisualDescendants().OfType<TextBlock>().Single().Text!;

        [AvaloniaFact]
        public void ACollapsedRow_ShowsASquareForEachKindItHolds_WithTheNumberInIt()
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
        public void AnOpenRow_ShowsNoSquares_AndAnEmptyRowShowsNone()
        {
            var (view, _, layer, empty) = Show();

            Assert.False(BadgesOf(view, layer).IsEffectivelyVisible);

            empty.IsExpanded = false;
            Settle();
            Assert.False(BadgesOf(view, empty).IsEffectivelyVisible);
            Assert.Empty(SquaresOf(BadgesOf(view, empty)));
        }

        [AvaloniaFact]
        public void TheSquares_FollowTheOpeningAndTheClosingOfTheRow()
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
        public void ACollapsedGroup_ShowsItsCount_InTheColorOfItsKind()
        {
            var (view, _, layer, _) = Show();
            var entities = layer.Children.Single(child => child.Header == "Entities");
            entities.IsExpanded = false;
            Settle();

            var square = Assert.Single(SquaresOf(BadgesOf(view, entities)));

            Assert.Equal("2", NumberIn(square));
            Assert.Equal(palette.For(TreeCategory.Entity).Kind, FillOf(square));
        }

        [AvaloniaFact]
        public void TheSquares_AreBrighterWhenTheRowIsSelected_AndInBetweenUnderThePointer()
        {
            var (view, tree, layer, _) = Show();
            layer.IsExpanded = false;
            Settle();
            var badges = BadgesOf(view, layer);
            var tone = palette.For(TreeCategory.Entity);

            Assert.Equal(tone.Kind, FillOf(SquaresOf(badges).First()));

            badges.IsHovered = true;
            Assert.Equal(tone.KindHover, FillOf(SquaresOf(badges).First()));

            tree.SelectedNode = layer;
            Settle();
            Assert.Equal(tone.KindSelected, FillOf(SquaresOf(badges).First()));
        }

        [AvaloniaFact]
        public void ASquare_IsSixteenHighInARowOfTwentyFour_SoTheSpaceAboveAndBelowIsTheSame()
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

        [AvaloniaFact]
        public void TheSquares_FollowTheNameOfTheRow()
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
