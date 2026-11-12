using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using static CMiX.Studio.Avalonia.Tests.Support.TreeTestScreen;

namespace CMiX.Studio.Avalonia.Tests
{
    // Each row draws a line in the middle of each indent step. The lines of the rows above the cursor row are darker.
    public class ProjectTreeIndentGuidesTests : IDisposable
    {
        private const double Indent = 16;

        private Window? window;

        public void Dispose() => window?.Close();

        // Composition A with two layers, composition B with one.
        private (ProjectTreeView View, Scene Scene) Show()
        {
            var scene = CreateScene();
            var screen = TreeTestScreen.Show(scene.Tree, 500, 800);
            window = screen.Window;
            scene.Tree.SelectedNode = null;
            scene.Tree.CursorNode = null;
            Settle();
            return (screen.View, scene);
        }

        private void Settle() => TreeTestScreen.Settle(window!);

        private static TreeIndentGuides GuidesOf(ProjectTreeView view, TreeNode node) =>
            ItemOf(view, node).GetVisualDescendants().OfType<TreeIndentGuides>().First(guides => ReferenceEquals(guides.DataContext, node));

        private static List<IBrush?> BrushesOf(TreeIndentGuides guides) => Enumerable.Range(0, guides.GuideCount).Select(guides.GuideBrush).ToList();

        // The left of line i of the guides, in the space of a visual.
        private static double LineLeft(TreeIndentGuides guides, int i, Visual relativeTo) =>
            guides.TranslatePoint(new Point(guides.GuideRect(i).X, 0), relativeTo)!.Value.X;

        private static IBrush? Resource(string key) => (IBrush?)Application.Current!.FindResource(key)!;

        private static double LeftOf(Visual visual, Visual relativeTo) => visual.TranslatePoint(new Point(0, 0), relativeTo)!.Value.X;

        [AvaloniaFact]
        public void Show_RowsAtDepthOneToThree_DrawALineInTheMiddleOfEachIndentStep()
        {
            var (view, scene) = Show();
            var composition = scene.NodeA;
            var layers = Scene.LayersOf(composition);
            var layer = scene.LayerNode;

            foreach (var (node, depth) in new[] { (composition, 1), (layers, 2), (layer, 3) })
            {
                var item = ItemOf(view, node);
                var guides = GuidesOf(view, node);

                Assert.Equal(depth, item.Level);
                Assert.Equal(depth, guides.GuideCount);
                for (var i = 0; i < depth; i++)
                {
                    Assert.Equal(i * Indent + Indent / 2, LineLeft(guides, i, item));
                    Assert.Equal(1, guides.GuideRect(i).Width);
                    // The line goes through the row and the space under it, so the lines of two rows join.
                    Assert.Equal(item.GetVisualDescendants().OfType<Grid>().First(grid => grid.Name == "RowGrid").Bounds.Height
                        + Part(view, node, "GapUnderTheRow").Bounds.Height, guides.GuideRect(i).Height, 3);
                }
            }

            // The line of a step is under the middle of the chevron of the row that owns it.
            var chevron = ItemOf(view, layers).GetVisualDescendants().OfType<ToggleButton>().First(toggle => toggle.Name == "PART_ExpandCollapseChevron");
            var chevronMiddle = chevron.TranslatePoint(new Point(chevron.Bounds.Width / 2, 0), view)!.Value.X;
            Assert.Equal(chevronMiddle, LineLeft(GuidesOf(view, layer), 2, view));
        }

        [AvaloniaFact]
        public void Show_IndentGuides_DoNotMoveTheRowAndTakeNoClick()
        {
            var (view, scene) = Show();

            foreach (var node in new[] { scene.NodeA, Scene.LayersOf(scene.NodeA), scene.LayerNode })
            {
                var item = ItemOf(view, node);
                var guides = GuidesOf(view, node);

                Assert.Equal((item.Level + 1) * Indent, LeftOf(RowOf(view, node), item));
                Assert.Equal(default, guides.DesiredSize);
                Assert.False(guides.IsHitTestVisible);
            }
        }

        [AvaloniaFact]
        public void CursorNode_ARow_DrawsTheLinesOfItsBranchDarker()
        {
            var (view, scene) = Show();
            var quiet = Resource("IndentGuideBrush");
            var active = Resource("IndentGuideActiveBrush");
            var layer = scene.LayerNode;
            var entities = Scene.Group<EntitiesGroup>(layer);
            var layerOfB = scene.LayerNodeOfB(0);

            Assert.All(BrushesOf(GuidesOf(view, entities)), brush => Assert.Same(quiet, brush));

            scene.Tree.CursorNode = layer;
            Settle();

            // The rows above the layer own the first three lines of its groups. The layer owns the fourth.
            Assert.Equal(new[] { active, active, active, quiet }, BrushesOf(GuidesOf(view, entities)));
            // Composition B is on another branch: only the line of the top row is darker.
            Assert.Equal(new[] { active, quiet, quiet }, BrushesOf(GuidesOf(view, layerOfB)));

            scene.Tree.CursorNode = layerOfB;
            Settle();

            Assert.Equal(new[] { active, quiet, quiet, quiet }, BrushesOf(GuidesOf(view, entities)));
            Assert.Equal(new[] { active, active, active }, BrushesOf(GuidesOf(view, layerOfB)));
        }

        [AvaloniaFact]
        public void CursorNode_Change_MarksOnlyTheRowsAboveTheCursor()
        {
            var scene = CreateScene();
            var layer = scene.LayerNode;

            scene.Tree.CursorNode = layer;
            Assert.True(Scene.LayersOf(scene.NodeA).IsOnCursorBranch);
            Assert.True(scene.NodeA.IsOnCursorBranch);
            Assert.False(layer.IsOnCursorBranch);
            Assert.False(scene.NodeB.IsOnCursorBranch);

            scene.Tree.CursorNode = scene.NodeB;
            Assert.False(Scene.LayersOf(scene.NodeA).IsOnCursorBranch);
            Assert.False(scene.NodeA.IsOnCursorBranch);
            Assert.True(scene.Tree.Roots[0].IsOnCursorBranch);
        }
    }
}
