using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The selected row has a thin border in the bright color of its kind. It is one pixel of the screen, and it takes no room.
    public class ProjectTreeSelectionFrameTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        private void Settle() => TreeTestScreen.Settle(window!);

        private (ProjectTreeView View, ProjectTree Tree) Show(double scale = 1.0)
        {
            var tree = CreateScene([2], s => s.Layer.ModelEntityManager.AddItem(typeof(Entity))).Tree;
            var screen = TreeTestScreen.Show(tree, 500, 700, scale);
            window = screen.Window;
            tree.SelectedNode = null;
            Settle();
            return (screen.View, tree);
        }

        [AvaloniaFact]
        public void Select_ARow_GivesOnlyThatRowTheFrame()
        {
            var (view, tree) = Show();
            var layer = tree.CompositionNodes()[0].Children[0].Children[0];
            var other = tree.CompositionNodes()[0].Children[0].Children[1];

            Assert.False(Part(view, layer, "SelectionFrame").IsEffectivelyVisible);

            tree.SelectedNode = layer;
            Settle();

            Assert.True(Part(view, layer, "SelectionFrame").IsEffectivelyVisible);
            Assert.False(Part(view, other, "SelectionFrame").IsEffectivelyVisible);
            Assert.False(Part(view, tree.CompositionNodes()[0], "SelectionFrame").IsEffectivelyVisible);
        }

        [AvaloniaFact]
        public void Select_ARow_GivesTheFrameTheBrightColorOfItsKind()
        {
            var (view, tree) = Show();
            var palette = CategoryPalette.FromApplication();

            foreach (var node in new[] { tree.CompositionNodes()[0], tree.CompositionNodes()[0].Children[0].Children[0], tree.CompositionNodes()[0].Children[0].Children[0].Children[0] })
            {
                tree.SelectedNode = node;
                Settle();

                var frame = Part(view, node, "SelectionFrame");
                Assert.Equal(palette.For(node.Category!.Value, node.ComposableDepth, node.KindDepth).KindSelected, ((ISolidColorBrush)frame.BorderBrush!).Color);
            }
        }

        [AvaloniaTheory]
        [InlineData(1.0)]
        [InlineData(1.25)]
        [InlineData(1.5)]
        [InlineData(2.0)]
        public void Select_AtEveryScaling_GivesAFrameOfOneScreenPixel(double scale)
        {
            var (view, tree) = Show(scale);
            var layer = tree.CompositionNodes()[0].Children[0].Children[0];
            tree.SelectedNode = layer;
            Settle();

            var thickness = Part(view, layer, "SelectionFrame").BorderThickness;

            Assert.Equal(1 / scale, thickness.Left, 6);
            Assert.Equal(thickness.Left, thickness.Top);
            Assert.Equal(thickness.Left, thickness.Right);
            Assert.Equal(thickness.Left, thickness.Bottom);
        }

        [AvaloniaFact]
        public void Select_ARow_CoversItExactlyAndKeepsItInPlace()
        {
            var (view, tree) = Show();
            var layer = tree.CompositionNodes()[0].Children[0].Children[0];
            var row = Part(view, layer, "RowBorder");
            var before = (row.Bounds, row.TranslatePoint(new Point(0, 0), window!));

            tree.SelectedNode = layer;
            Settle();

            var frame = Part(view, layer, "SelectionFrame");
            Assert.Equal(before, (row.Bounds, row.TranslatePoint(new Point(0, 0), window!)));
            Assert.Equal(row.Bounds.Size, frame.Bounds.Size);
            Assert.False(frame.IsHitTestVisible);
        }

        [AvaloniaFact]
        public void Select_ARow_GivesTheFrameTheCornersOfTheRow()
        {
            var (view, tree) = Show();
            var layer = tree.CompositionNodes()[0].Children[0].Children[0];

            Assert.Equal(Part(view, layer, "RowBorder").CornerRadius, Part(view, layer, "SelectionFrame").CornerRadius);
        }
    }
}
