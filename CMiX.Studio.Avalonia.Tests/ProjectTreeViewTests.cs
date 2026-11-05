using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Prefabs;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.String;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Entity = CMiX.Core.Compositing.Entity;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The tree of the project on screen: one row per node, and a drag that follows the rules of TreeDropService.
    public class ProjectTreeViewTests
    {
        private sealed class Screen
        {
            public Window Window { get; init; } = null!;
            public ProjectTreeView View { get; init; } = null!;
            public ProjectTree Tree { get; init; } = null!;
            public Project Project { get; init; } = null!;
            public Composition A { get; init; } = null!;
            public Composition B { get; init; } = null!;

            public TreeView TreeView => View.GetVisualDescendants().OfType<TreeView>().First();

            public TreeViewItem RowOf(TreeNode node) =>
                View.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

            // A point in the header of a row, as a fraction of its height.
            public Point PointIn(TreeNode node, double fraction)
            {
                var header = RowOf(node).GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");
                return header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height * fraction), Window)!.Value;
            }

            public void Drag(TreeNode source, TreeNode target, double fraction, RawDragEventType last)
            {
                var data = new DataObject();
                data.Set(ProjectTreeView.NodeFormat, source);

                // The hit test of the window reads the last rendered scene. After a change of the rows it must be rendered first.
                Window.UpdateLayout();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();

                var point = PointIn(target, fraction);
                var effects = DragDropEffects.Move;
                Window.DragDrop(point, RawDragEventType.DragEnter, data, effects);
                Window.DragDrop(point, RawDragEventType.DragOver, data, effects);
                if (last == RawDragEventType.Drop)
                    Window.DragDrop(point, RawDragEventType.Drop, data, effects);
                Dispatcher.UIThread.RunJobs();
            }
        }

        // A holds two layers, B holds one.
        private static Screen Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            project.CompositionManager.AddItem(typeof(Composition));
            var a = (Composition)project.CompositionManager.ManagerData.Items[0];
            var b = (Composition)project.CompositionManager.ManagerData.Items[1];
            a.LayerManager.AddItem(typeof(Layer));
            a.LayerManager.AddItem(typeof(Layer));
            b.LayerManager.AddItem(typeof(Layer));

            var tree = new ProjectTree(project, provider.GetRequiredService<UndoManager>());
            var view = new ProjectTreeView { DataContext = tree };
            var window = new Window { Content = view, Width = 400, Height = 1100 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return new Screen { Window = window, View = view, Tree = tree, Project = project, A = a, B = b };
        }

        private static int CountNodes(TreeNode node) => 1 + node.Children.Sum(CountNodes);

        [AvaloniaFact]
        public void TheView_ShowsOneRowPerNode()
        {
            var screen = Show();

            var rows = screen.View.GetVisualDescendants().OfType<TreeViewItem>().Count();

            Assert.Equal(screen.Tree.Roots.Sum(CountNodes), rows);
        }

        private static Border HeaderOf(TreeViewItem row) =>
            row.GetVisualDescendants().OfType<Border>().First(border => border.Name == "RowBorder");

        // The toggles of a row: the ones in its toggle column, from the left. The column of the first row found is its own.
        private static List<ToggleButton> TogglesOf(TreeViewItem row) =>
            row.GetVisualDescendants().OfType<RowToggles>().First()
                .GetVisualDescendants().OfType<ToggleButton>()
                .Where(toggle => toggle.IsEffectivelyVisible).ToList();

        private static IEnumerable<TreeNode> AllNodes(IEnumerable<TreeNode> nodes) =>
            nodes.SelectMany(node => new[] { node }.Concat(AllNodes(node.Children)));

        [AvaloniaFact]
        public void EveryRow_HasTheHeightOfARowOfTheOtherLists()
        {
            var screen = Show();
            screen.A.LayerManager.ManagerData.Items.OfType<Layer>().First().ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();
            var expected = Convert.ToDouble(Application.Current!.FindResource("ControlHeightMD"));

            var heights = AllNodes(screen.Tree.Roots).Select(node => HeaderOf(screen.RowOf(node)).Bounds.Height).ToList();

            Assert.NotEmpty(heights);
            Assert.All(heights, height => Assert.Equal(expected, height));
        }

        [AvaloniaTheory]
        [InlineData(0.1)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void ADragOverARow_MovesNoRow(double fraction)
        {
            var screen = Show();
            var nodes = AllNodes(screen.Tree.Roots).ToList();
            var before = nodes.Select(node => HeaderOf(screen.RowOf(node)).TranslatePoint(new Point(0, 0), screen.Window)).ToList();
            var heightsBefore = nodes.Select(node => HeaderOf(screen.RowOf(node)).Bounds.Height).ToList();

            screen.Drag(screen.Tree.Roots[0].Children[0], screen.Tree.Roots[1], fraction, RawDragEventType.DragOver);

            Assert.Equal(before, nodes.Select(node => HeaderOf(screen.RowOf(node)).TranslatePoint(new Point(0, 0), screen.Window)));
            Assert.Equal(heightsBefore, nodes.Select(node => HeaderOf(screen.RowOf(node)).Bounds.Height));
        }

        [AvaloniaFact]
        public void TwoRowsNextToEachOther_AreOneRowGapApart()
        {
            var screen = Show();
            var spacing = Convert.ToDouble(screen.View.Resources["TreeRowGap"]);
            var layerNode = screen.Tree.Roots[0].Children[0];
            var firstGroup = HeaderOf(screen.RowOf(layerNode.Children[0]));
            var secondGroup = HeaderOf(screen.RowOf(layerNode.Children[1]));

            var gap = secondGroup.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y
                - (firstGroup.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y + firstGroup.Bounds.Height);

            Assert.Equal(3, spacing);
            Assert.Equal(spacing, gap);
        }

        [AvaloniaFact]
        public void AChildRow_IsOneRowGapBelowItsParentRow()
        {
            var screen = Show();
            var spacing = Convert.ToDouble(screen.View.Resources["TreeRowGap"]);
            var parent = HeaderOf(screen.RowOf(screen.Tree.Roots[0]));
            var child = HeaderOf(screen.RowOf(screen.Tree.Roots[0].Children[0]));

            var gap = child.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y
                - (parent.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y + parent.Bounds.Height);

            Assert.Equal(spacing, gap);
        }

        private static TreeNode LastVisibleOf(TreeNode node) =>
            node.IsExpanded && node.Children.Count > 0 ? LastVisibleOf(node.Children[^1]) : node;

        // The own mark line of an item: the last one in the tree order, because the rows under the item come before it.
        private static Border DropLineOf(TreeViewItem row) =>
            row.GetVisualDescendants().OfType<Border>().Last(border => border.Name == "DropLine");

        private static double Top(Screen screen, Control control) => control.TranslatePoint(new Point(0, 0), screen.Window)!.Value.Y;

        // Three compositions. The last one holds a layer, so its block is the composition, the layer and the three groups of the layer.
        private static Screen ShowWithABlock()
        {
            var screen = Show();
            screen.Project.CompositionManager.AddItem(typeof(Composition));
            ((Composition)screen.Project.CompositionManager.ManagerData.Items[2]).LayerManager.AddItem(typeof(Layer));
            Dispatcher.UIThread.RunJobs();
            screen.Window.UpdateLayout();
            return screen;
        }

        [AvaloniaTheory]
        [InlineData(0.05)]
        [InlineData(0.4)]
        public void ACompositionOverTheTopHalfOfACompositionRow_ShowsTheDropBefore(double fraction)
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];

            screen.Drag(screen.Tree.Roots[0], last, fraction, RawDragEventType.DragOver);

            Assert.Contains(":dropbefore", screen.RowOf(last).Classes);
        }

        [AvaloniaTheory]
        [InlineData(0.6)]
        [InlineData(0.95)]
        public void ACompositionOverTheBottomHalfOfTheLastRowOfABlock_ShowsTheDropAfterTheWholeBlock(double fraction)
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];
            var lastRow = LastVisibleOf(last);
            Assert.NotSame(last, lastRow);

            screen.Drag(screen.Tree.Roots[0], lastRow, fraction, RawDragEventType.DragOver);

            Assert.Contains(":dropafter", screen.RowOf(last).Classes);
            Assert.DoesNotContain(":dropafter", screen.RowOf(lastRow).Classes);
        }

        [AvaloniaTheory]
        [InlineData(0.6)]
        [InlineData(0.95)]
        public void ACompositionOverTheBottomHalfOfAnExpandedCompositionRow_ShowsNothing(double fraction)
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];

            screen.Drag(screen.Tree.Roots[0], last, fraction, RawDragEventType.DragOver);

            Assert.DoesNotContain(":dropbefore", screen.RowOf(last).Classes);
            Assert.DoesNotContain(":dropafter", screen.RowOf(last).Classes);
            Assert.DoesNotContain(":droptarget", screen.RowOf(last).Classes);
        }

        [AvaloniaTheory]
        [InlineData(0.1)]
        [InlineData(0.9)]
        public void ACompositionOverAMiddleRowOfABlock_ShowsNothing(double fraction)
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];

            screen.Drag(screen.Tree.Roots[0], last.Children[0], fraction, RawDragEventType.DragOver);

            Assert.DoesNotContain(":dropbefore", screen.RowOf(last).Classes);
            Assert.DoesNotContain(":dropafter", screen.RowOf(last).Classes);
        }

        [AvaloniaFact]
        public void ACollapsedCompositionRow_HasBothHalves()
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];
            last.IsExpanded = false;
            Dispatcher.UIThread.RunJobs();
            screen.Window.UpdateLayout();

            screen.Drag(screen.Tree.Roots[0], last, 0.9, RawDragEventType.DragOver);

            Assert.Contains(":dropafter", screen.RowOf(last).Classes);
        }

        [AvaloniaFact]
        public void TheAfterBar_IsDrawnUnderTheLastRowOfTheBlock()
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];
            var lastRow = LastVisibleOf(last);

            screen.Drag(screen.Tree.Roots[0], lastRow, 0.9, RawDragEventType.DragOver);

            var line = DropLineOf(screen.RowOf(lastRow));
            var lastRowHeader = HeaderOf(screen.RowOf(lastRow));
            var gap = Convert.ToDouble(screen.View.Resources["TreeRowGap"]);
            Assert.True(line.IsVisible);
            // The bar fills the space under the last row of the block.
            Assert.Equal(Top(screen, lastRowHeader) + lastRowHeader.Bounds.Height, Top(screen, line), 3);
            Assert.Equal(gap, line.Bounds.Height, 3);
        }

        [AvaloniaFact]
        public void TheBeforeBar_IsDrawnAtTheTopOfTheBlock()
        {
            var screen = ShowWithABlock();
            var last = screen.Tree.Roots[2];

            screen.Drag(screen.Tree.Roots[0], last, 0.2, RawDragEventType.DragOver);

            var above = HeaderOf(screen.RowOf(LastVisibleOf(screen.Tree.Roots[1])));
            var below = HeaderOf(screen.RowOf(last));
            var line = DropLineOf(screen.RowOf(LastVisibleOf(screen.Tree.Roots[1])));
            Assert.True(line.IsVisible);
            // The bar fills the space between the two rows: the same space above it and under it.
            Assert.Equal(Top(screen, above) + above.Bounds.Height, Top(screen, line), 3);
            Assert.Equal(Top(screen, below), Top(screen, line) + line.Bounds.Height, 3);
        }

        [AvaloniaFact]
        public void ALayerOverTheBottomHalfOfTheLastRowOfAnotherLayer_ShowsTheDropAfterThatLayer()
        {
            var screen = Show();
            var first = screen.Tree.Roots[0].Children[0];
            var second = screen.Tree.Roots[0].Children[1];
            var lastRow = LastVisibleOf(second);
            Assert.NotSame(second, lastRow);

            screen.Drag(first, lastRow, 0.9, RawDragEventType.DragOver);

            Assert.Contains(":dropafter", screen.RowOf(second).Classes);
        }

        [AvaloniaFact]
        public void ALayerOverAMiddleRowOfAnotherLayer_ShowsNothing()
        {
            var screen = Show();
            var first = screen.Tree.Roots[0].Children[0];
            var second = screen.Tree.Roots[0].Children[1];

            screen.Drag(first, second.Children[0], 0.9, RawDragEventType.DragOver);

            Assert.DoesNotContain(":dropafter", screen.RowOf(second).Classes);
            Assert.DoesNotContain(":dropbefore", screen.RowOf(second).Classes);
        }

        [AvaloniaTheory]
        [InlineData(0.05)]
        [InlineData(0.5)]
        [InlineData(0.95)]
        public void ALayerOverACompositionRow_ShowsTheDropInsideWhereverThePointerIs(double fraction)
        {
            var screen = Show();

            screen.Drag(screen.Tree.Roots[0].Children[0], screen.Tree.Roots[1], fraction, RawDragEventType.DragOver);

            Assert.Contains(":droptarget", screen.RowOf(screen.Tree.Roots[1]).Classes);
        }

        private static TextBoxEditable NameBoxOf(TreeViewItem row) =>
            row.GetVisualDescendants().OfType<TextBoxEditable>().First();

        private static bool IsRenaming(TextBoxEditable box) =>
            box.GetVisualDescendants().OfType<TextBox>().Any(input => input.IsVisible);

        // A point in the name box of a row, to the right of the text of the name.
        private static Point AwayFromTheName(Screen screen, TreeViewItem row)
        {
            var box = NameBoxOf(row);
            return box.TranslatePoint(new Point(box.Bounds.Width - 12, box.Bounds.Height / 2), screen.Window)!.Value;
        }

        private static void DoubleClick(Screen screen, Point point)
        {
            // The hit test of the window reads the last rendered scene.
            screen.Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();

            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            screen.Window.MouseDown(point, MouseButton.Left);
            screen.Window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaTheory]
        [InlineData(true)]
        [InlineData(false)]
        public void ADoubleClickAwayFromTheName_ExpandsOrCollapsesTheRow_AndStartsNoRename(bool expanded)
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];
            node.IsExpanded = expanded;
            Dispatcher.UIThread.RunJobs();
            var row = screen.RowOf(node);

            DoubleClick(screen, AwayFromTheName(screen, row));

            Assert.Equal(!expanded, row.IsExpanded);
            Assert.Equal(!expanded, node.IsExpanded);
            Assert.False(IsRenaming(NameBoxOf(row)));
        }

        [AvaloniaFact]
        public void ADoubleClickOnTheIconOfARow_ExpandsOrCollapsesTheRow()
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];
            var row = screen.RowOf(node);
            var icon = row.GetVisualDescendants().OfType<CMiX.UI.Controls.AppIcon>().First();
            var point = icon.TranslatePoint(new Point(icon.Bounds.Width / 2, icon.Bounds.Height / 2), screen.Window)!.Value;

            DoubleClick(screen, point);

            Assert.False(row.IsExpanded);
        }

        [AvaloniaFact]
        public void ADoubleClickAwayFromTheName_OfARowWithoutChildren_DoesNothing()
        {
            var screen = Show();
            // A row without children: a modulator of a modifier of an entity (the entity, the modifier and its groups have rows under them).
            var layer = screen.A.LayerManager.ManagerData.Items.OfType<Layer>().First();
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var modifiers = ((Entity)layer.ModelEntityManager.ManagerData.Items[0]).ModifierManager;
            modifiers.AddItem(typeof(CMiX.Core.Modulation.Modifiers.TranslateModifier));
            ((CMiX.Core.Modulation.IModulatable)modifiers.ManagerData.Items[0]).ModulatorManager.AddItem(typeof(CMiX.Core.Modulation.Modulators.LFOModulator));
            Dispatcher.UIThread.RunJobs();
            var modifierNode = screen.Tree.Roots[0].Children[0].Children[0].Children[0].Children.Single(group => group.Header == "Modifiers").Children[0];
            var entityNode = modifierNode.Children.Single(group => group.Header == "Modulators").Children[0];
            for (var ancestor = entityNode.Parent; ancestor != null; ancestor = ancestor.Parent)
                ancestor.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            var entityRow = screen.RowOf(entityNode);
            var before = entityNode.IsExpanded;

            DoubleClick(screen, AwayFromTheName(screen, entityRow));

            Assert.False(IsRenaming(NameBoxOf(entityRow)));
            Assert.Equal(before, entityNode.IsExpanded);
            Assert.True(screen.RowOf(screen.Tree.Roots[0]).IsExpanded);
        }

        [AvaloniaFact]
        public void ADoubleClickOnAToggle_DoesNotExpandOrCollapseTheRow()
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];
            var row = screen.RowOf(node);
            var visibility = ToggleWithTheme(row, "ToggleButtonVisibility");
            var before = screen.A.PrefabService.Visibility.Value;
            var point = visibility.TranslatePoint(new Point(visibility.Bounds.Width / 2, visibility.Bounds.Height / 2), screen.Window)!.Value;

            DoubleClick(screen, point);

            Assert.True(row.IsExpanded);
            Assert.Equal(before, screen.A.PrefabService.Visibility.Value);
        }

        [AvaloniaFact]
        public void ADoubleClickInTheInputWhileRenaming_KeepsTheRenameAndTheRow()
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];
            var row = screen.RowOf(node);
            var box = NameBoxOf(row);
            DoubleClick(screen, box.TranslatePoint(new Point(10, box.Bounds.Height / 2), screen.Window)!.Value);
            Assert.True(IsRenaming(box));

            DoubleClick(screen, box.TranslatePoint(new Point(box.Bounds.Width - 12, box.Bounds.Height / 2), screen.Window)!.Value);

            Assert.True(IsRenaming(box));
            Assert.True(row.IsExpanded);
        }

        [AvaloniaTheory]
        [InlineData(true)]
        [InlineData(false)]
        public void ADoubleClickOnTheName_StartsTheRenameAndKeepsTheRowAsItWas(bool expanded)
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];
            node.IsExpanded = expanded;
            Dispatcher.UIThread.RunJobs();
            var row = screen.RowOf(node);
            var box = NameBoxOf(row);
            var point = box.TranslatePoint(new Point(10, box.Bounds.Height / 2), screen.Window)!.Value;

            DoubleClick(screen, point);

            Assert.Equal(expanded, row.IsExpanded);
            Assert.Equal(expanded, node.IsExpanded);
            Assert.Contains(box.GetVisualDescendants().OfType<TextBox>(), input => input.IsVisible);
        }

        [AvaloniaFact]
        public void TheNameBox_FillsTheColoredRowAfterTheIcon()
        {
            var screen = Show();
            var row = screen.RowOf(screen.Tree.Roots[0]);
            var content = row.GetVisualDescendants().OfType<Grid>().First(grid => grid.Name == "RowContent");
            var box = NameBoxOf(row);
            var colored = HeaderOf(row);

            var boxRight = box.TranslatePoint(new Point(box.Bounds.Width, 0), screen.Window)!.Value.X;
            var coloredRight = colored.TranslatePoint(new Point(colored.Bounds.Width, 0), screen.Window)!.Value.X;

            // The box reaches the end of the colored row.
            Assert.True(coloredRight - boxRight <= 4, $"The name box ends {coloredRight - boxRight} px before the end of the row.");
            Assert.True(box.Bounds.Width > content.Bounds.Width / 2, "The name box is less than half of the row.");
        }

        [AvaloniaFact]
        public void TheChevron_ExpandsAndCollapsesTheRow()
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];
            var row = screen.RowOf(node);
            var chevron = row.GetVisualDescendants().OfType<ToggleButton>().First(toggle => toggle.Name == "PART_ExpandCollapseChevron");

            chevron.IsChecked = false;
            Dispatcher.UIThread.RunJobs();
            Assert.False(row.IsExpanded);
            Assert.False(node.IsExpanded);

            chevron.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.True(row.IsExpanded);
            Assert.True(node.IsExpanded);
        }

        [AvaloniaFact]
        public void ThePointerOverARow_HighlightsOnlyThatRowAndNotItsParent()
        {
            var screen = Show();
            var composition = screen.Tree.Roots[0];
            var layer = composition.Children[0];

            screen.Window.MouseMove(screen.PointIn(layer, 0.5));
            Dispatcher.UIThread.RunJobs();

            Assert.True(HeaderOf(screen.RowOf(layer)).IsPointerOver);
            Assert.False(HeaderOf(screen.RowOf(composition)).IsPointerOver);
        }

        [AvaloniaFact]
        public void ACompositionRowAndALayerRow_HaveTheVisibilityToggleAndTheMaskToggle()
        {
            var screen = Show();

            Assert.Equal(2, TogglesOf(screen.RowOf(screen.Tree.Roots[0])).Count);
            Assert.Equal(2, TogglesOf(screen.RowOf(screen.Tree.Roots[0].Children[0])).Count);
        }

        private static ToggleButton ToggleWithTheme(TreeViewItem row, string themeKey) =>
            TogglesOf(row).Single(toggle => ReferenceEquals(toggle.Theme, Application.Current!.FindResource(themeKey)));

        private static double RightEdge(Screen screen, Control control) =>
            control.TranslatePoint(new Point(0, 0), screen.Window)!.Value.X + control.Bounds.Width;

        private static double LeftEdge(Screen screen, Control control) =>
            control.TranslatePoint(new Point(0, 0), screen.Window)!.Value.X;

        [AvaloniaFact]
        public void TheVisibilityToggle_IsFirst_AndTheMaskToggleIsRightOfIt()
        {
            var screen = Show();

            foreach (var node in new[] { screen.Tree.Roots[0], screen.Tree.Roots[0].Children[0] })
            {
                var row = screen.RowOf(node);
                var visibility = ToggleWithTheme(row, "ToggleButtonVisibility");
                var mask = ToggleWithTheme(row, "ToggleButtonMask");

                Assert.True(RightEdge(screen, visibility) <= mask.TranslatePoint(new Point(0, 0), screen.Window)!.Value.X,
                    $"The mask toggle of {node.Header} is not right of the visibility toggle.");
            }
        }

        [AvaloniaFact]
        public void TheVisibilityToggleOfEveryRow_HasTheSameLeftEdge()
        {
            var screen = Show();
            screen.A.LayerManager.ManagerData.Items.OfType<Layer>().First().ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();
            screen.Window.UpdateLayout();
            var composition = screen.Tree.Roots[0];
            var layer = composition.Children[0];
            var entity = layer.Children[0].Children[0];

            var edges = new[] { composition, layer, entity }
                .Select(node => LeftEdge(screen, ToggleWithTheme(screen.RowOf(node), "ToggleButtonVisibility")))
                .ToList();

            Assert.All(edges, edge => Assert.Equal(edges[0], edge));
        }

        [AvaloniaFact]
        public void AnEntityRow_HasTheVisibilityToggleOnly_AndAGroupRowHasNone()
        {
            var screen = Show();
            screen.A.LayerManager.ManagerData.Items.OfType<Layer>().First().ModelEntityManager.AddItem(typeof(Entity));
            Dispatcher.UIThread.RunJobs();
            var layerNode = screen.Tree.Roots[0].Children[0];
            var group = layerNode.Children[0];

            Assert.Single(TogglesOf(screen.RowOf(group.Children[0])));
            Assert.Empty(TogglesOf(screen.RowOf(group)));
        }

        [AvaloniaTheory]
        [InlineData("ToggleButtonVisibility")]
        [InlineData("ToggleButtonMask")]
        public void TheToggles_ChangeTheVisibilityAndTheMaskOfTheComposition(string themeKey)
        {
            var screen = Show();
            var visibility = themeKey == "ToggleButtonVisibility";
            var toggle = ToggleWithTheme(screen.RowOf(screen.Tree.Roots[0]), themeKey);
            var before = visibility ? screen.A.PrefabService.Visibility.Value : screen.A.Mask.IsMask.Value;

            toggle.IsChecked = !before;
            Dispatcher.UIThread.RunJobs();

            var after = visibility ? screen.A.PrefabService.Visibility.Value : screen.A.Mask.IsMask.Value;
            Assert.Equal(!before, after);
        }

        [AvaloniaFact]
        public void ARowOfAGroup_ShowsTheNameOfTheGroup()
        {
            var screen = Show();
            var group = screen.Tree.Roots[0].Children[0].Children[0];

            var texts = screen.RowOf(group).GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text);

            Assert.Contains("Entities", texts);
        }

        [AvaloniaFact]
        public void ASelectedRow_SelectsTheNode()
        {
            var screen = Show();
            var node = screen.Tree.Roots[0].Children[1];

            screen.TreeView.SelectedItem = node;

            Assert.Same(node, screen.Tree.SelectedNode);
            Assert.Same(screen.A.LayerManager.ManagerData.Items[1], screen.A.LayerManager.SelectedItem);
        }

        [AvaloniaFact]
        public void ADragOverTheLowerPartOfARow_MarksTheRowAndAnEndedDragClearsIt()
        {
            var screen = Show();
            var row = screen.RowOf(screen.Tree.Roots[1]);
            var lastRow = LastVisibleOf(screen.Tree.Roots[1]);

            screen.Drag(screen.Tree.Roots[0], lastRow, 0.9, RawDragEventType.DragOver);
            Assert.Contains(":dropafter", row.Classes);

            screen.Window.DragDrop(screen.PointIn(lastRow, 0.9), RawDragEventType.DragLeave,
                new DataObject(), DragDropEffects.Move);
            Dispatcher.UIThread.RunJobs();
            Assert.DoesNotContain(":dropafter", row.Classes);
        }

        [AvaloniaFact]
        public void ADragOverTheMiddleOfACompositionRow_MarksItAsInside()
        {
            var screen = Show();
            var layerNode = screen.Tree.Roots[0].Children[0];

            screen.Drag(layerNode, screen.Tree.Roots[1], 0.5, RawDragEventType.DragOver);

            Assert.Contains(":droptarget", screen.RowOf(screen.Tree.Roots[1]).Classes);
        }

        [AvaloniaTheory]
        [InlineData(0.1)]
        [InlineData(0.9)]
        public void ADragOverTheItemItself_MarksNothing(double fraction)
        {
            var screen = Show();
            var node = screen.Tree.Roots[0];

            screen.Drag(node, node, fraction, RawDragEventType.DragOver);

            Assert.DoesNotContain(":dropbefore", screen.RowOf(node).Classes);
            Assert.DoesNotContain(":dropafter", screen.RowOf(node).Classes);
            Assert.DoesNotContain(":droptarget", screen.RowOf(node).Classes);
        }

        [AvaloniaFact]
        public void ADragOverThePlaceWhereTheItemAlreadyIs_MarksNothing()
        {
            var screen = Show();
            var next = screen.Tree.Roots[1];

            // The first composition before the second one: the same place.
            screen.Drag(screen.Tree.Roots[0], next, 0.1, RawDragEventType.DragOver);

            Assert.DoesNotContain(":dropbefore", screen.RowOf(next).Classes);
        }

        [AvaloniaTheory]
        [InlineData(0.5, true)]
        [InlineData(0.1, false)]
        public void ADragOfANodeOfTheTree_ShowsTheMoveCursorAndNeverTheCursorForNotAllowed(double fraction, bool allowed)
        {
            var screen = Show();
            var effects = new List<DragDropEffects>();
            screen.Window.AddHandler(DragDrop.DragOverEvent, (_, e) => effects.Add(e.DragEffects), RoutingStrategies.Bubble, handledEventsToo: true);
            var source = screen.Tree.Roots[0];
            var target = allowed ? screen.Tree.Roots[1] : source;

            screen.Drag(source, target, fraction, RawDragEventType.DragOver);

            Assert.NotEmpty(effects);
            Assert.All(effects, effect => Assert.Equal(DragDropEffects.Move, effect));
        }

        [AvaloniaFact]
        public void ADragOverATargetThatIsNotAllowed_MarksNothing()
        {
            var screen = Show();
            var group = screen.Tree.Roots[0].Children[0].Children[0];

            screen.Drag(screen.Tree.Roots[0], group, 0.5, RawDragEventType.DragOver);

            Assert.DoesNotContain(":droptarget", screen.RowOf(group).Classes);
            Assert.DoesNotContain(":dropbefore", screen.RowOf(group).Classes);
            Assert.DoesNotContain(":dropafter", screen.RowOf(group).Classes);
        }

        [AvaloniaFact]
        public void ADropOnTheBottomHalfOfTheLastRowOfACompositionBlock_ReordersTheCompositions()
        {
            var screen = Show();
            var nodeA = screen.Tree.Roots[0];
            var nodeB = screen.Tree.Roots[1];

            screen.Drag(nodeA, LastVisibleOf(nodeB), 0.9, RawDragEventType.Drop);

            Assert.Equal(new IControl[] { screen.B, screen.A }, screen.Project.CompositionManager.ManagerData.Items);
            Assert.DoesNotContain(":dropafter", screen.RowOf(nodeB).Classes);
        }

        [AvaloniaFact]
        public void ADropOfALayerOnTheMiddleOfACompositionRow_MovesTheLayerThere()
        {
            var screen = Show();
            var moved = screen.A.LayerManager.ManagerData.Items[0];

            screen.Drag(screen.Tree.Roots[0].Children[0], screen.Tree.Roots[1], 0.5, RawDragEventType.Drop);

            Assert.Single(screen.A.LayerManager.ManagerData.Items);
            Assert.Same(moved, screen.B.LayerManager.ManagerData.Items[1]);
        }

        [AvaloniaFact]
        public void ADropOfALayerWithASharedEntity_ChangesNothing()
        {
            var screen = Show();
            var layer = (Layer)screen.A.LayerManager.ManagerData.Items[0];
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (IPrefab)layer.ModelEntityManager.ManagerData.Items[0];
            ((Layer)screen.A.LayerManager.ManagerData.Items[1]).ModelEntityManager.AddExistingItem(entity);

            screen.Drag(screen.Tree.Roots[0].Children[0], screen.Tree.Roots[1], 0.5, RawDragEventType.Drop);

            Assert.Equal(2, screen.A.LayerManager.ManagerData.Items.Count);
            Assert.Single(screen.B.LayerManager.ManagerData.Items);
        }

        [AvaloniaFact]
        public void AGroup_CannotBeDragged_ButAnItemCan()
        {
            var screen = Show();
            var layerNode = screen.Tree.Roots[0].Children[0];
            ((Layer)screen.A.LayerManager.ManagerData.Items[0]).ModelEntityManager.AddItem(typeof(Entity));

            foreach (var group in layerNode.Children)
                Assert.False(screen.Tree.CanDrag(group));
            Assert.True(screen.Tree.CanDrag(screen.Tree.Roots[0]));
            Assert.True(screen.Tree.CanDrag(layerNode));
            Assert.True(screen.Tree.CanDrag(layerNode.Children[0].Children[0]));
        }

        // The press on a row remembers the node for a drag. A group is not remembered, so a move of the pointer starts no drag.
        [AvaloniaFact]
        public void APressOnAGroup_PreparesNoDrag_AndAPressOnALayerDoes()
        {
            var screen = Show();
            var layerNode = screen.Tree.Roots[0].Children[0];
            var field = typeof(ProjectTreeView).GetField("pressedNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            screen.Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();

            screen.Window.MouseDown(screen.PointIn(layerNode.Children[0], 0.5), MouseButton.Left);
            Assert.Null(field.GetValue(screen.View));
            screen.Window.MouseUp(screen.PointIn(layerNode.Children[0], 0.5), MouseButton.Left);

            screen.Window.MouseDown(screen.PointIn(layerNode, 0.5), MouseButton.Left);
            Assert.Same(layerNode, field.GetValue(screen.View));
            screen.Window.MouseUp(screen.PointIn(layerNode, 0.5), MouseButton.Left);
        }

        [AvaloniaFact]
        public void ADropOnATargetThatIsNotAllowed_ChangesNothing()
        {
            var screen = Show();
            var group = screen.Tree.Roots[0].Children[0].Children[0];

            screen.Drag(screen.Tree.Roots[1], group, 0.5, RawDragEventType.Drop);

            Assert.Equal(new IControl[] { screen.A, screen.B }, screen.Project.CompositionManager.ManagerData.Items);
        }
    }
}
