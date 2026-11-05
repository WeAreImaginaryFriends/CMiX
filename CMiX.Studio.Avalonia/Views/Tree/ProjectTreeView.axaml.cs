// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;
using CompositionModel = CMiX.Core.Compositing.Composition;
using LayerModel = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Views
{
    // The tree of the project. A node is dragged to reorder it in its list.
    // A layer can also go to another composition. TreeDropService holds the rules.
    public partial class ProjectTreeView : UserControl
    {
        public const string NodeFormat = "CMiX.TreeNode";

        private const double DragThreshold = 4.0;
        private const string DropBefore = ":dropbefore";
        private const string DropAfter = ":dropafter";
        private const string DropTarget = ":droptarget";
        private const string DropGap = ":dropgap";
        private const string DropTop = ":droptop";
        private TreeViewItem? barItem;

        private TreeNode? pressedNode;
        private Point pressPoint;
        private TreeViewItem? marked;
        private DropPosition? markedPosition;

        public ProjectTreeView()
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(treeView, true);
            treeView.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
            treeView.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            treeView.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            treeView.AddHandler(DragDrop.DropEvent, OnDrop);
            treeView.AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Bubble);
            treeView.AddHandler(DoubleTappedEvent, OnDoubleTapped, RoutingStrategies.Bubble);
        }

        // The tree does not scroll by itself when the user chooses a row: a click on a row that is at the edge of the view would scroll to
        // show the whole block of the row, and the second click of a double click would land on another row. A row that a program or a
        // peer selects is brought into view here, and only its own row, not the rows under it.
        private ProjectTree? watched;
        private bool chosenByClick;

        // A row asks to be brought into view when it gets the focus or the selection. For the choice of the user the request stops here:
        // it would scroll to show the whole block of the row.
        static ProjectTreeView()
        {
            RequestBringIntoViewEvent.AddClassHandler<TreeViewItem>((item, e) =>
            {
                if (item.FindAncestorOfType<ProjectTreeView>() is { chosenByClick: true })
                    e.Handled = true;
            });
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (watched != null)
                watched.PropertyChanged -= OnWatchedPropertyChanged;

            watched = DataContext as ProjectTree;
            if (watched != null)
                watched.PropertyChanged += OnWatchedPropertyChanged;
        }

        private void OnWatchedPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProjectTree.SelectedNode) || chosenByClick || watched?.SelectedNode is not { } node)
                return;

            Dispatcher.UIThread.Post(() => RevealRow(node), DispatcherPriority.Loaded);
        }

        private void RevealRow(TreeNode node)
        {
            var item = treeView.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(row => ReferenceEquals(row.DataContext, node));
            item?.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Name == "RowBorder")?.BringIntoView();
        }

        // The height of a row and the space between two rows, in whole pixels of the screen.
        // On a screen with a scaling like 125%, a size of 24 or of 1 is not a whole number of pixels, and every panel rounds its
        // size up. The rows of a long tree are then slightly more than one pitch apart, and the error adds up. With whole pixels
        // there is nothing to round, and every row is exactly one pitch from the next, whatever is open.
        // The height goes up to the next pixel, and the space is one pixel or more.
        public static (double Height, double Spacing) SnapToPixels(double height, double spacing, double scale)
        {
            var heightPixels = Math.Ceiling(height * scale - 1e-6);
            var spacingPixels = Math.Max(1, Math.Round(spacing * scale, MidpointRounding.AwayFromZero));
            return (heightPixels / scale, spacingPixels / scale);
        }

        // The space between two rows, one above the other, at a screen of 100%. The drop line (SplitterIndicatorSize) sits in it.
        public const double RowGap = 3;

        private void SetRowMetrics(double scale)
        {
            var height = Convert.ToDouble(Application.Current!.FindResource("ControlHeightMD"));
            var spacing = Convert.ToDouble(Application.Current!.FindResource("SpacingXXS"));
            var (snappedHeight, snappedSpacing) = SnapToPixels(height, spacing, scale);
            Resources["TreeRowHeight"] = snappedHeight;
            Resources["TreeRowSpacing"] = snappedSpacing;
            Resources["TreeToggleWidth"] = RowToggles.WidthFor(snappedHeight, snappedSpacing);

            // The space between rows is a space of its own, not the space between a row and its toggles.
            var (_, snappedGap) = SnapToPixels(height, RowGap, scale);
            Resources["TreeRowGap"] = snappedGap;

            // The drop line in the space under a row starts at the end of the row and at the start of the colored row (16).
            Resources["TreeDropLineGapMargin"] = new Thickness(16, snappedHeight, 0, 0);

            // The border of the selected row is one pixel of the screen, so it is as thin on a screen of 150% as on a screen of 100%.
            Resources["TreeSelectionThickness"] = new Thickness(1 / scale);
        }

        private TopLevel? scaledTopLevel;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            scaledTopLevel = TopLevel.GetTopLevel(this);
            if (scaledTopLevel == null)
                return;

            SetRowMetrics(scaledTopLevel.RenderScaling);
            scaledTopLevel.ScalingChanged += OnScalingChanged;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (scaledTopLevel != null)
                scaledTopLevel.ScalingChanged -= OnScalingChanged;

            scaledTopLevel = null;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnScalingChanged(object? sender, EventArgs e) => SetRowMetrics(scaledTopLevel?.RenderScaling ?? 1);

        // A double tap on a row, away from its name, expands or collapses the row. The name takes its own double tap (it starts
        // the rename and handles the tap), so that tap never comes here. A toggle or an input keeps its double tap too.
        // TreeViewItem does this by itself for the part PART_HeaderPresenter. The template has no such part (see ProjectTree.axaml),
        // so the tree does it here, and only where it is wanted.
        private void OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (IsEditor(e.Source) || ItemOf(e.Source)?.DataContext is not TreeNode { Children.Count: > 0 } node)
                return;

            node.IsExpanded = !node.IsExpanded;
            e.Handled = true;
        }

        // The menu of the row under the pointer, built and opened when it is asked for. A menu that is set during the event
        // does not open on that event, so it is opened here.
        // The tree listens in the bubble phase, as the menu of a row of a list does. A name that is being edited
        // blocks the request in the tunnel phase (ContextMenuGuard), so a right click during a rename opens no menu.
        // The menu belongs to the tree and no row holds one. A row with a menu would open it for the right clicks on its
        // children too, because a request goes up through every parent row.
        private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            treeView.ContextMenu?.Close();

            // On a row: the menu of the row. On the space under the rows: a menu to add a composition, which is under no row.
            var item = ItemOf(e.Source);
            var menu = item?.DataContext is TreeNode node && DataContext is ProjectTree tree ? CreateMenu(item, node, tree)
                : item == null && DataContext is ProjectTree project ? CreateSpaceMenu(project)
                : null;
            treeView.ContextMenu = menu;
            if (menu == null)
                return;

            menu.Open(treeView);
            e.Handled = true;
        }

        // The menu of the empty space of the tree: the rows that the project takes.
        private ContextMenu CreateSpaceMenu(ProjectTree tree) =>
            PrefabRowMenu.Create(treeView, RowMenuKind.Group, () => null, tree.GetAdds(null).ToRows(), () => "Project");

        private static ContextMenu? CreateMenu(TreeViewItem row, TreeNode node, ProjectTree tree)
        {
            var adds = tree.GetAdds(node).ToRows();
            var kind = node.Item == null ? RowMenuKind.Group
                : node.Item is CompositionModel or LayerModel ? RowMenuKind.Composable
                : RowMenuKind.Slot;

            if (kind == RowMenuKind.Group && adds.Count == 0)
                return null;

            return PrefabRowMenu.Create(
                row,
                kind,
                () => node.Owner != null && node.Item != null ? (node.Owner, node.Item) : null,
                adds,
                () => node.Header);
        }

        private static TreeViewItem? ItemOf(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is TreeViewItem item)
                    return item;

            return null;
        }

        private static bool IsInToggles(object? source) =>
            (source as Visual)?.FindAncestorOfType<RowToggles>(true) != null;

        private static bool IsEditor(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is ToggleButton or TextBox)
                    return true;

            return false;
        }

        // A left click on a row, away from its buttons and its name box: the user chooses the node. It does not come when a node is
        // selected by a program, a list or a peer. The main window uses it to show the inspector.
        public static readonly RoutedEvent<NodeChosenEventArgs> NodeChosenEvent =
            RoutedEvent.Register<ProjectTreeView, NodeChosenEventArgs>(nameof(NodeChosen), RoutingStrategies.Bubble);

        // A routed event: it goes up to the window, whichever panel the tree is in.
        public event EventHandler<NodeChosenEventArgs> NodeChosen
        {
            add => AddHandler(NodeChosenEvent, value);
            remove => RemoveHandler(NodeChosenEvent, value);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            pressedNode = null;

            // The selection that this press makes is the choice of the user: the tree does not scroll for it.
            chosenByClick = true;
            Dispatcher.UIThread.Post(() => chosenByClick = false, DispatcherPriority.Background);
            if (!e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed || IsEditor(e.Source))
                return;

            var node = ItemOf(e.Source)?.DataContext as TreeNode;
            // The toggles at the right of a row never start a drag, not even the space around them.
            pressedNode = node != null && !IsInToggles(e.Source) && DataContext is ProjectTree tree && tree.CanDrag(node) ? node : null;
            pressPoint = e.GetPosition(treeView);

            if (node != null)
                RaiseEvent(new NodeChosenEventArgs(NodeChosenEvent, this, node));
        }

        private async void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (pressedNode == null || !e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed)
                return;

            var distance = e.GetPosition(treeView) - pressPoint;
            if (Math.Abs(distance.X) < DragThreshold && Math.Abs(distance.Y) < DragThreshold)
                return;

            var node = pressedNode;
            pressedNode = null;

            var data = new DataObject();
            data.Set(NodeFormat, node);

            // The tree shows every row that can take the node for as long as the drag goes on.
            var tree = DataContext as ProjectTree;
            tree?.BeginDrag(node);
            try
            {
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
            finally
            {
                tree?.EndDrag();
                StopAutoScroll();
                Mark(null, null);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => pressedNode = null;

        // The local paths of the files and folders of a drag from the file explorer. A drag of a node has none.
        private static List<string> PathsOf(DragEventArgs e) =>
            (e.Data.GetFiles() ?? Enumerable.Empty<IStorageItem>())
                .Select(item => item.TryGetLocalPath())
                .Where(path => path != null && (File.Exists(path) || Directory.Exists(path)))
                .Select(path => path!)
                .ToList();

        // Files over the tree: the row under the pointer looks hovered when that row takes the files itself (see
        // ProjectTree.IsPathTarget). Over a child of a layer, or over the space under the rows, nothing is marked.
        private void OnFilesDragOver(DragEventArgs e)
        {
            e.Handled = true;
            e.DragEffects = DragDropEffects.None;
            Mark(null, null);

            if (DataContext is not ProjectTree tree || !tree.CanDropPaths(PathsOf(e)))
                return;

            e.DragEffects = DragDropEffects.Copy;
            var hoveredItem = ItemOf(e.Source);
            if (hoveredItem?.DataContext is TreeNode hovered && tree.IsPathTarget(hovered))
                Mark(hoveredItem, DropPosition.Inside);
        }

        private void OnFilesDrop(DragEventArgs e)
        {
            e.Handled = true;
            Mark(null, null);

            if (DataContext is ProjectTree tree)
                tree.DropPaths(ItemOf(e.Source)?.DataContext as TreeNode, PathsOf(e));
        }

        // The tree scrolls while a drag is near its top or bottom edge. The system gives no mouse wheel during a drag,
        // so a timer scrolls as long as the pointer stays in the edge zone.
        private const double EdgeZone = 28.0;
        private const double MaxScrollStep = 18.0;
        private readonly DispatcherTimer autoScrollTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
        private double? dragY;

        private ScrollViewer? ScrollerOfTree =>
            treeView.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        private void TrackDragPointer(DragEventArgs e)
        {
            dragY = ScrollerOfTree is { } scroller ? e.GetPosition(scroller).Y : null;
            wheelHook ??= new DragWheelHook(steps => Dispatcher.UIThread.Post(() => ScrollByWheel(steps)));
            wheelHook.Start();
            if (!autoScrollTimer.IsEnabled)
            {
                autoScrollTimer.Tick -= OnAutoScrollTick;
                autoScrollTimer.Tick += OnAutoScrollTick;
                autoScrollTimer.Start();
            }
        }

        private void StopAutoScroll()
        {
            autoScrollTimer.Stop();
            wheelHook?.Stop();
            dragY = null;
        }

        // The mouse wheel also scrolls the tree during a drag (see DragWheelHook). A step is three rows.
        private DragWheelHook? wheelHook;

        public void ScrollByWheel(int steps)
        {
            if (dragY == null || ScrollerOfTree is not { } scroller)
                return;

            var maxOffset = Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height);
            var rows = steps * 3 * (Convert.ToDouble(Resources["TreeRowHeight"]) + Convert.ToDouble(Resources["TreeRowGap"]));
            scroller.Offset = scroller.Offset.WithY(Math.Clamp(scroller.Offset.Y - rows, 0, maxOffset));
        }

        private void OnAutoScrollTick(object? sender, EventArgs e) => AutoScrollStep();

        // One step of the scroll of a drag. The step is larger when the pointer is closer to the edge, or beyond it.
        // Returns the distance that the view moved.
        public double AutoScrollStep()
        {
            if (dragY is not { } y || ScrollerOfTree is not { } scroller)
                return 0;

            var step = 0.0;
            if (y < EdgeZone)
                step = -MaxScrollStep * Math.Min(1, (EdgeZone - y) / EdgeZone);
            else if (y > scroller.Bounds.Height - EdgeZone)
                step = MaxScrollStep * Math.Min(1, (y - (scroller.Bounds.Height - EdgeZone)) / EdgeZone);

            if (step == 0)
                return 0;

            var before = scroller.Offset.Y;
            var maxOffset = Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height);
            scroller.Offset = scroller.Offset.WithY(Math.Clamp(before + step, 0, maxOffset));
            return scroller.Offset.Y - before;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            TrackDragPointer(e);

            if (e.Data.Contains(DataFormats.Files))
            {
                OnFilesDragOver(e);
                return;
            }

            e.DragEffects = DragDropEffects.None;
            e.Handled = true;

            var (source, item, target, position) = Read(e);

            // A node of the tree always shows the move cursor. The cursor for a drop that is not possible is a red icon,
            // which does not fit the look of the app. The mark tells where a drop works: no mark, no move.
            if (source != null)
                e.DragEffects = DragDropEffects.Move;

            if (DataContext is not ProjectTree tree || source == null || item == null || target == null || position == null
                || !tree.CanDrop(source, target, position.Value))
            {
                Mark(null, null);
                return;
            }

            Mark(item, position);
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            // A leave also comes each time the pointer goes from one row to the next. The scroll stops only when the pointer is out of the tree.
            if (!new Rect(treeView.Bounds.Size).Contains(e.GetPosition(treeView)))
                StopAutoScroll();

            Mark(null, null);
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            StopAutoScroll();

            if (e.Data.Contains(DataFormats.Files))
            {
                OnFilesDrop(e);
                return;
            }

            e.Handled = true;
            Mark(null, null);

            var (source, _, target, position) = Read(e);
            if (DataContext is not ProjectTree tree || source == null || target == null || position == null)
                return;

            await tree.DropAsync(source, target, position.Value);
        }

        // The dragged node, the item that takes the drop, its node, and the place of the drop. The item that takes the drop
        // is the row under the pointer or one of its parents (see ProjectTree.Locate). It has no place when no drop is offered here.
        private (TreeNode? Source, TreeViewItem? Item, TreeNode? Target, DropPosition? Position) Read(DragEventArgs e)
        {
            var source = e.Data.Get(NodeFormat) as TreeNode;
            var hoveredItem = ItemOf(e.Source);
            if (source == null || hoveredItem?.DataContext is not TreeNode hovered || DataContext is not ProjectTree tree)
                return (source, null, null, null);

            var header = hoveredItem.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Name == "RowBorder");
            var fraction = header == null || header.Bounds.Height <= 0
                ? 0.5
                : e.GetPosition(header).Y / header.Bounds.Height;

            // The pointer is in the space between two rows, not on a row: the drop stays where it was.
            if ((fraction < 0 || fraction > 1) && marked != null && marked.DataContext is TreeNode markedNode && markedPosition != null)
                return (source, marked, markedNode, markedPosition.Value);

            if (tree.Locate(source, hovered, fraction) is not { } place)
                return (source, null, null, null);

            return (source, ItemOfNode(hoveredItem, place.Target), place.Target, place.Position);
        }

        // The item of a node: the item of the row under the pointer, or the first of its parent items that has the node.
        private static TreeViewItem? ItemOfNode(TreeViewItem from, TreeNode node)
        {
            for (var item = from; item != null; item = item.GetVisualAncestors().OfType<TreeViewItem>().FirstOrDefault())
                if (ReferenceEquals(item.DataContext, node))
                    return item;

            return null;
        }

        private void Mark(TreeViewItem? item, DropPosition? position)
        {
            if (marked != null)
            {
                var old = (IPseudoClasses)marked.Classes;
                old.Set(DropBefore, false);
                old.Set(DropAfter, false);
                old.Set(DropTarget, false);
            }

            if (barItem != null)
            {
                var oldBar = (IPseudoClasses)barItem.Classes;
                oldBar.Set(DropGap, false);
                oldBar.Set(DropTop, false);
                barItem = null;
            }

            marked = item;
            markedPosition = position;
            if (item == null || position == null)
                return;

            ((IPseudoClasses)item.Classes).Set(
                position == DropPosition.Before ? DropBefore : position == DropPosition.After ? DropAfter : DropTarget,
                true);

            if (position == DropPosition.Inside || item.DataContext is not TreeNode node)
                return;

            // The bar is in the space under the row above the place: the last visible row of the block for a drop after the node.
            var above = position == DropPosition.After ? TreeDropService.LastVisible(node) : TreeDropService.RowAbove(node);
            var aboveItem = above == null ? null : ItemOfNode(item, above) ?? FindItem(above);
            barItem = aboveItem ?? item;
            ((IPseudoClasses)barItem.Classes).Set(aboveItem != null ? DropGap : DropTop, true);
        }

        private TreeViewItem? FindItem(TreeNode node) =>
            treeView.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(item => ReferenceEquals(item.DataContext, node));
    }
}

namespace CMiX.Studio.Avalonia.Views
{
    // The node that the user chose with a click on its row.
    public sealed class NodeChosenEventArgs : RoutedEventArgs
    {
        public NodeChosenEventArgs(RoutedEvent routedEvent, object source, TreeNode node)
            : base(routedEvent, source)
        {
            Node = node;
        }

        public TreeNode Node { get; }
    }
}
