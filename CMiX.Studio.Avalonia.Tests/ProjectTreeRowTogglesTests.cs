using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The toggles of a row are on their own squares, at the right of the colored row and outside it. The visibility toggle is the
    // first square and the mask toggle follows it.
    public class ProjectTreeRowTogglesTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        private static void SetScaling(Window target, double scale)
        {
            var impl = target.PlatformImpl!;
            impl.GetType().GetField("<RenderScaling>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(impl, scale);
            ((Action<double>?)impl.GetType().GetProperty("ScalingChanged")!.GetValue(impl))?.Invoke(scale);
        }

        private void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window!.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        // A composition with a layer, which has an entity.
        private (ProjectTreeView View, ProjectTree Tree) Show(double scale = 1.0)
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            composition.LayerManager.AddItem(typeof(Layer));
            ((Layer)composition.LayerManager.ManagerData.Items[0]).ModelEntityManager.AddItem(typeof(Entity));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 600 };
            window.Show();
            SetScaling(window, scale);
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            Settle();
            return (view, tree);
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static Border RowOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");

        private static RowToggles ColumnOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<RowToggles>().First();

        private static List<Border> SquaresOf(RowToggles column) =>
            column.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("slot") && border.IsEffectivelyVisible).ToList();

        private Rect BoundsIn(Control control) => new(control.TranslatePoint(new Point(0, 0), window!)!.Value, control.Bounds.Size);

        private static IEnumerable<TreeNode> All(IEnumerable<TreeNode> nodes) =>
            nodes.SelectMany(node => new[] { node }.Concat(All(node.Children)));

        [AvaloniaFact]
        public void TheSquares_AreOutsideTheColoredRow_AtItsRight()
        {
            var (view, tree) = Show();

            foreach (var node in All(tree.Roots).Where(node => node.Item != null))
            {
                var row = BoundsIn(RowOf(view, node));
                foreach (var square in SquaresOf(ColumnOf(view, node)))
                    Assert.True(BoundsIn(square).Left >= row.Right, $"A toggle of {node.Header} is inside the colored row.");
            }
        }

        [AvaloniaFact]
        public void TheEye_IsTheFirstSquare_AndTheMaskFollowsIt()
        {
            var (view, tree) = Show();
            var composition = tree.Roots[0];
            var squares = SquaresOf(ColumnOf(view, composition));

            Assert.Equal(2, squares.Count);
            var eye = squares[0].GetVisualDescendants().OfType<ToggleButton>().Single();
            var mask = squares[1].GetVisualDescendants().OfType<ToggleButton>().Single();
            Assert.Same(Application.Current!.FindResource("ToggleButtonVisibility"), eye.Theme);
            Assert.Same(Application.Current!.FindResource("ToggleButtonMask"), mask.Theme);
            Assert.True(BoundsIn(squares[0]).Right <= BoundsIn(squares[1]).Left);
        }

        [AvaloniaFact]
        public void AnEntity_HasOnlyTheEye_AtTheSamePlaceAsTheEyeOfAComposition_AndEveryOtherSquareIsEmpty()
        {
            var (view, tree) = Show();
            var composition = tree.Roots[0];
            var layer = composition.Children[0];
            var entities = layer.Children[0];
            var entity = entities.Children[0];

            // A group has two empty squares, an entity has the eye and an empty square. A square without a button is only a place.
            var groupSquares = SquaresOf(ColumnOf(view, entities));
            Assert.Equal(2, groupSquares.Count);
            Assert.All(groupSquares, square => Assert.Empty(square.GetVisualDescendants().OfType<ToggleButton>().Where(button => button.IsEffectivelyVisible)));

            var entitySquares = SquaresOf(ColumnOf(view, entity));
            Assert.Equal(2, entitySquares.Count);
            Assert.Single(entitySquares[0].GetVisualDescendants().OfType<ToggleButton>().Where(button => button.IsEffectivelyVisible));
            Assert.Empty(entitySquares[1].GetVisualDescendants().OfType<ToggleButton>().Where(button => button.IsEffectivelyVisible));
            Assert.Equal(BoundsIn(SquaresOf(ColumnOf(view, composition))[0]).Left, BoundsIn(entitySquares[0]).Left);
            Assert.Equal(BoundsIn(SquaresOf(ColumnOf(view, composition))[1]).Left, BoundsIn(entitySquares[1]).Left);
        }

        [AvaloniaFact]
        public void EveryColoredRow_EndsAtTheSamePlace_WhateverItHolds()
        {
            var (view, tree) = Show();

            var rights = All(tree.Roots).Select(node => BoundsIn(RowOf(view, node)).Right).Distinct().ToList();

            Assert.Single(rights);
        }

        [AvaloniaTheory]
        [InlineData(1.0)]
        [InlineData(1.25)]
        [InlineData(1.5)]
        public void TheSpaces_AreTheSpaceOfTheRows_AndEverySquareIsARowHigh(double scale)
        {
            var (view, tree) = Show(scale);
            var composition = tree.Roots[0];
            var row = BoundsIn(RowOf(view, composition));
            var squares = SquaresOf(ColumnOf(view, composition)).Select(BoundsIn).ToList();
            var space = Convert.ToDouble(((Control)view).Resources["TreeRowSpacing"]);

            Assert.Equal(space, squares[0].Left - row.Right, 3);
            Assert.Equal(space, squares[1].Left - squares[0].Right, 3);
            Assert.All(squares, square =>
            {
                Assert.Equal(row.Height, square.Height, 3);
                Assert.Equal(row.Height, square.Width, 3);
                Assert.Equal(row.Top, square.Top, 3);
            });
        }

        [AvaloniaFact]
        public void TheToggle_IsInTheMiddleOfItsSquare_WithTheSameSpaceOnEverySide()
        {
            var (view, tree) = Show();
            var composition = tree.Roots[0];

            foreach (var square in SquaresOf(ColumnOf(view, composition)))
            {
                var toggle = BoundsIn(square.GetVisualDescendants().OfType<ToggleButton>().Single());
                var box = BoundsIn(square);

                Assert.Equal(toggle.Left - box.Left, box.Right - toggle.Right, 3);
                Assert.Equal(toggle.Top - box.Top, box.Bottom - toggle.Bottom, 3);
                Assert.Equal(toggle.Left - box.Left, toggle.Top - box.Top, 3);
                Assert.True(toggle.Left - box.Left > 0);
            }
        }

        [AvaloniaFact]
        public void TheSquares_HaveTheBackgroundOfTheSecondPanelLevel()
        {
            var (view, tree) = Show();
            var expected = ((ISolidColorBrush)Application.Current!.FindResource("PanelBackgroundLevel2Brush")!).Color;

            foreach (var square in SquaresOf(ColumnOf(view, tree.Roots[0])))
                Assert.Equal(expected, ((ISolidColorBrush)square.Background!).Color);
        }

        [AvaloniaFact]
        public void ASquareWithNoButton_IsDarker_ThanASquareWithOne()
        {
            var (view, tree) = Show();
            var darkest = ((ISolidColorBrush)Application.Current!.FindResource("Grey100Brush")!).Color;
            var level2 = ((ISolidColorBrush)Application.Current!.FindResource("PanelBackgroundLevel2Brush")!).Color;
            var entities = tree.Roots[0].Children[0].Children[0];
            var entity = entities.Children[0];

            // A group has two empty squares. An entity has the eye on a normal square, then an empty square.
            Assert.All(SquaresOf(ColumnOf(view, entities)), square => Assert.Equal(darkest, ((ISolidColorBrush)square.Background!).Color));
            var entitySquares = SquaresOf(ColumnOf(view, entity));
            Assert.Equal(level2, ((ISolidColorBrush)entitySquares[0].Background!).Color);
            Assert.Equal(darkest, ((ISolidColorBrush)entitySquares[1].Background!).Color);
        }

        [AvaloniaFact]
        public void TheColumn_HasTheWidthOfTheSpaceAndAllTheSquares()
        {
            var (view, tree) = Show();
            var column = ColumnOf(view, tree.Roots[0]);
            var row = BoundsIn(RowOf(view, tree.Roots[0]));

            Assert.Equal(RowToggles.WidthFor(row.Height, Convert.ToDouble(view.Resources["TreeRowSpacing"])), column.Bounds.Width, 3);
        }
    }
}
