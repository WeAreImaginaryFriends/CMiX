using CMiX.Core.Texturing.Filters;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;

namespace CMiX.Studio.Avalonia.Tests
{
    // A row that closes over the cursor (the soft selection) takes the cursor, so the cursor is never on a hidden row.
    // The selection does not change.
    public class ProjectTreeCursorCloseTests
    {
        // A with two layers. Each layer holds an Echo filter. Every row is open.
        private static Scene Scene()
        {
            var scene = CreateScene([2], s => s.Layer.TextureModifierManager.AddItem(typeof(Echo)));
            scene.LayerOfA(1).TextureModifierManager.AddItem(typeof(Echo));
            foreach (var node in scene.All())
                node.IsExpanded = true;
            return scene;
        }

        private static TreeNode EchoOf(Scene scene, int layer) =>
            scene.All().Single(node => node.Item is Echo && ReferenceEquals(node.Item, scene.LayerOfA(layer).TextureModifierManager.ManagerData.Items[0]));

        private static void AssertCursorShows(Scene scene) =>
            Assert.Contains(scene.Tree.SoftSelectedNode!, scene.Tree.VisibleNodes());

        [Fact]
        public void Collapse_CursorUnder_MovesCursorToTheRow()
        {
            var scene = Scene();
            var selected = scene.LayerNodeOfA(0);
            scene.Tree.SelectedNode = selected;
            scene.Tree.SoftSelectedNode = EchoOf(scene, 1);

            scene.LayerNodeOfA(1).IsExpanded = false;

            Assert.Same(scene.LayerNodeOfA(1), scene.Tree.SoftSelectedNode);
            Assert.Same(selected, scene.Tree.SelectedNode);
        }

        [Fact]
        public void Collapse_CursorNotUnder_KeepsTheCursor()
        {
            var scene = Scene();
            var cursor = EchoOf(scene, 0);
            scene.Tree.SoftSelectedNode = cursor;

            scene.LayerNodeOfA(1).IsExpanded = false;

            Assert.Same(cursor, scene.Tree.SoftSelectedNode);
        }

        [Fact]
        public void Collapse_TheCursorRow_KeepsTheCursor()
        {
            var scene = Scene();
            var cursor = scene.LayerNodeOfA(1);
            scene.Tree.SoftSelectedNode = cursor;

            cursor.IsExpanded = false;

            Assert.Same(cursor, scene.Tree.SoftSelectedNode);
        }

        // Two parents of the cursor close, in either order. The cursor ends on the higher one, the row that shows.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Collapse_NestedRows_MovesCursorToTheHighestClosedRow(bool innerFirst)
        {
            var scene = Scene();
            var echo = EchoOf(scene, 1);
            var inner = echo.Parent!;
            var outer = scene.LayerNodeOfA(1);
            Assert.NotSame(inner, outer);
            scene.Tree.SoftSelectedNode = echo;

            foreach (var row in innerFirst ? new[] { inner, outer } : new[] { outer, inner })
                row.IsExpanded = false;

            Assert.Same(outer, scene.Tree.SoftSelectedNode);
            AssertCursorShows(scene);
        }

        // The end of the search closes the rows that it opened. The cursor under them goes to the highest closed row.
        [Fact]
        public void Clear_TheSearch_MovesTheCursorToTheHighestClosedRow()
        {
            var scene = Scene();
            foreach (var node in scene.All())
                node.IsExpanded = false;
            scene.Tree.SelectedNode = null;
            scene.Tree.SearchText = "Echo";
            var echo = EchoOf(scene, 1);
            Assert.Contains(echo, scene.Tree.VisibleNodes());
            scene.Tree.SoftSelectedNode = echo;

            scene.Tree.SearchText = string.Empty;

            Assert.NotSame(echo, scene.Tree.SoftSelectedNode);
            Assert.True(echo.IsOrIsUnder(scene.Tree.SoftSelectedNode!));
            Assert.False(scene.Tree.SoftSelectedNode!.IsExpanded);
            AssertCursorShows(scene);
            Assert.Null(scene.Tree.SelectedNode);
        }

        // CollapseOthers keeps the path to the cursor open, so the cursor stays on its row. A cursor under the selected row does
        // not count: the cursor is the target.
        [Fact]
        public void CollapseOthers_ACursorDeepInTheTree_KeepsTheCursorOnItsRow()
        {
            var scene = Scene();
            var selected = EchoOf(scene, 0);
            scene.Tree.SelectedNode = selected;
            var cursor = EchoOf(scene, 1);
            scene.Tree.SoftSelectedNode = cursor;

            var closed = scene.Tree.CollapseOthers();

            Assert.NotEmpty(closed);
            Assert.Same(cursor, scene.Tree.SoftSelectedNode);
            Assert.Same(selected, scene.Tree.SelectedNode);
            AssertCursorShows(scene);
        }
    }
}
