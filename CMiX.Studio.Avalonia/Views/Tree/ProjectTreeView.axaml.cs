// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.Platform;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;
using CMiX.UI.Controls.String;
using CompositionModel = CMiX.Core.Compositing.Composition;
using LayerModel = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Views
{
    // The tree of the project. A node is dragged to reorder it in its list.
    // A layer can also go to another composition. TreeDropService holds the rules.
    public partial class ProjectTreeView : UserControl
    {
        public const string NodeFormat = "CMiX.TreeNode";

        // The space between two rows at a screen of 100% (see TreeRowMetrics).
        public const double RowGap = TreeRowMetrics.RowGap;

        private const double DragThreshold = 4.0;

        private readonly TreeRowMetrics rowMetrics;
        private readonly TreeDropMarker marker;
        private readonly TreeScrollAnchor scrollAnchor;

        private TreeNode? pressedNode;
        private Point pressPoint;

        public ProjectTreeView()
        {
            InitializeComponent();

            rowMetrics = new TreeRowMetrics(treeView, Resources, () => ScrollerOfTree);
            marker = new TreeDropMarker(treeView);
            scrollAnchor = new TreeScrollAnchor(treeView, () => ScrollerOfTree);

            DragDrop.SetAllowDrop(treeView, true);
            treeView.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
            treeView.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            treeView.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            treeView.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            treeView.AddHandler(DragDrop.DropEvent, OnDrop);
            treeView.AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Bubble);
            treeView.AddHandler(DoubleTappedEvent, OnDoubleTapped, RoutingStrategies.Bubble);
            treeView.AddHandler(ScrollViewer.ScrollChangedEvent, (_, _) => rowMetrics.SetEndRoom());
            treeView.TemplateApplied += (_, e) =>
                e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter")?.AddHandler(RequestBringIntoViewEvent, OnRequestBringIntoView);
            treeView.AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
            KeyBindingTextGuard.Install(this);
        }

        // The tree does not scroll by itself when the user clicks a row: a click on a row that is at the edge of the view would scroll to
        // show the whole block of the row, and the second click of a double click would land on another row. A row that a program or a
        // key selects is brought into view here (RevealRow), and only its own row, not the rows under it.
        private ProjectTree? watched;
        private bool attached;

        // True while the view drops a node. The drop keeps the view where it is (see OnDrop).
        private bool dropping;

        // The row border that RevealRow brings into view.
        private Visual? revealTarget;

        // A row asks to be brought into view when it gets the focus or the selection. That request scrolls to the whole block of the
        // row, so it stops here, under the scroller of the tree. Only the request of RevealRow goes through.
        private void OnRequestBringIntoView(object? sender, RequestBringIntoViewEventArgs e)
        {
            if (!ReferenceEquals(e.TargetObject, revealTarget))
                e.Handled = true;
        }

        // Avalonia runs the key bindings of the view (the gestures of the map) before this handler. The navigation keys move the soft
        // selection only (see ProjectTree.SoftSelectedNode). The tree view does not see them, so they select nothing. A navigation key
        // with a modifier does nothing. A key in a name box stays in the name box.
        private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
        {
            if (watched is not { } tree || IsInTextBox(e.Source) || !IsNavigationKey(e.Key))
                return;

            e.Handled = true;
            if (e.KeyModifiers != KeyModifiers.None)
                return;

            MoveSoftSelection(tree, e.Key);
            if (tree.SoftSelectedNode is { } node)
                Dispatcher.UIThread.Post(() => FocusRow(node), DispatcherPriority.Loaded);
        }

        private static bool IsNavigationKey(Key key) =>
            key is Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End or Key.Left or Key.Right;

        private void MoveSoftSelection(ProjectTree tree, Key key)
        {
            switch (key)
            {
                case Key.Up: tree.MoveSoftSelection(-1); break;
                case Key.Down: tree.MoveSoftSelection(1); break;
                case Key.PageUp: tree.MoveSoftSelection(-RowsInView()); break;
                case Key.PageDown: tree.MoveSoftSelection(RowsInView()); break;
                case Key.Home: tree.MoveSoftSelectionToEnd(false); break;
                case Key.End: tree.MoveSoftSelectionToEnd(true); break;
                case Key.Left: tree.MoveSoftSelectionOut(); break;
                case Key.Right: tree.MoveSoftSelectionIn(); break;
            }
        }

        private int RowsInView() =>
            ScrollerOfTree is { } scroller && rowMetrics.RowPitch > 0 ? Math.Max(1, (int)(scroller.Viewport.Height / rowMetrics.RowPitch)) : 1;

        // Moves the keyboard focus to the row of the node and brings the row into view. The focus shows the focus look of the row
        // (:focus-visible, see ProjectTree.axaml). It does not select the row.
        private void FocusRow(TreeNode node)
        {
            if (TreeRows.FindItem(treeView, node) is not { } item)
                return;

            item.Focus(NavigationMethod.Tab);
            RevealRow(node);
        }

        // Starts the rename of the row of the node: the name box of the row goes to edit mode.
        private void OnRenameRequested(object? sender, TreeNode node)
        {
            if (TreeRows.FindItem(treeView, node) is not { } item || TreeRows.RowBorderOf(item) is not { } row)
                return;

            if (row.GetVisualDescendants().OfType<TextBoxEditable>().FirstOrDefault() is { } box)
                box.IsEditing = true;
        }

        private static bool IsInTextBox(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is TextBox)
                    return true;

            return false;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (attached)
                Unwatch();

            watched = DataContext as ProjectTree;
            if (attached)
                Watch();
        }

        private void Watch()
        {
            if (watched == null)
                return;

            watched.PropertyChanged += OnWatchedPropertyChanged;
            watched.RenameRequested += OnRenameRequested;
        }

        private void Unwatch()
        {
            if (watched == null)
                return;

            watched.PropertyChanged -= OnWatchedPropertyChanged;
            watched.RenameRequested -= OnRenameRequested;
        }

        private void OnWatchedPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProjectTree.SelectedNode) || dropping || watched?.SelectedNode is not { } node
                || watched.Origin is not (SelectionOrigin.Program or SelectionOrigin.Peer))
                return;

            Dispatcher.UIThread.Post(() => RevealRow(node), DispatcherPriority.Loaded);
        }

        private void RevealRow(TreeNode node)
        {
            if (TreeRows.FindItem(treeView, node) is not { } item || TreeRows.RowBorderOf(item) is not { } border)
                return;

            revealTarget = border;
            try
            {
                border.BringIntoView();
            }
            finally
            {
                revealTarget = null;
            }
        }

        // See TreeRowMetrics.SnapToPixels.
        public static (double Height, double Spacing) SnapToPixels(double height, double spacing, double scale) =>
            TreeRowMetrics.SnapToPixels(height, spacing, scale);

        private TopLevel? scaledTopLevel;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // The tree view model listens only while the view is attached.
            attached = true;
            Watch();

            scaledTopLevel = TopLevel.GetTopLevel(this);
            if (scaledTopLevel == null)
                return;

            rowMetrics.SetRowMetrics(scaledTopLevel.RenderScaling);
            scaledTopLevel.ScalingChanged += OnScalingChanged;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Unwatch();

            attached = false;
            if (scaledTopLevel != null)
                scaledTopLevel.ScalingChanged -= OnScalingChanged;

            scaledTopLevel = null;
            autoScroller?.Dispose();
            autoScroller = null;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnScalingChanged(object? sender, EventArgs e) => rowMetrics.SetRowMetrics(scaledTopLevel?.RenderScaling ?? 1);

        // A double tap on a row, away from its name, expands or collapses the row. The name takes its own double tap (it starts
        // the rename and handles the tap), so that tap never comes here. A toggle or an input keeps its double tap too.
        // TreeViewItem does this by itself for the part PART_HeaderPresenter. The template has no such part (see ProjectTree.axaml),
        // so the tree does it here, and only where it is wanted.
        private void OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (IsEditor(e.Source) || TreeRows.ItemOf(e.Source)?.DataContext is not TreeNode { Children.Count: > 0 } node)
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
            var item = TreeRows.ItemOf(e.Source);
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

        private static bool IsInToggles(object? source) =>
            (source as Visual)?.FindAncestorOfType<RowToggles>(true) != null;

        private static bool IsEditor(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is ToggleButton or TextBox)
                    return true;

            return false;
        }

        // A left click on a row, away from its buttons and its name box: the user chooses the node. It comes on the release, when no drag
        // started and the pointer is still on the row. It does not come when a node is selected by a program, a list or a peer.
        // The main window uses it to show the inspector.
        public static readonly RoutedEvent<NodeChosenEventArgs> NodeChosenEvent =
            RoutedEvent.Register<ProjectTreeView, NodeChosenEventArgs>(nameof(NodeChosen), RoutingStrategies.Bubble);

        // A routed event: it goes up to the window, whichever panel the tree is in.
        public event EventHandler<NodeChosenEventArgs> NodeChosen
        {
            add => AddHandler(NodeChosenEvent, value);
            remove => RemoveHandler(NodeChosenEvent, value);
        }

        // The node of the row that the left button pressed. A drag clears it.
        private TreeNode? chosenNode;

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            pressedNode = null;
            chosenNode = null;
            if (!e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed || IsEditor(e.Source))
                return;

            var node = TreeRows.ItemOf(e.Source)?.DataContext as TreeNode;
            // The toggles at the right of a row never start a drag, not even the space around them.
            pressedNode = node != null && !IsInToggles(e.Source) && DataContext is ProjectTree tree && tree.CanDrag(node) ? node : null;
            pressPoint = e.GetPosition(treeView);
            chosenNode = node;
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
            chosenNode = null;

            var data = new DataObject();
            data.Set(NodeFormat, node);

            // The tree shows every row that can take the node for as long as the drag goes on.
            var tree = DataContext as ProjectTree;
            tree?.BeginDrag(node);
            try
            {
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ProjectTreeView: the drag failed. {exception}");
            }
            finally
            {
                ShowBlockReason(null);
                tree?.EndDrag();
                autoScroller?.StopNow();
                marker.Mark(null, null);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var node = chosenNode;
            pressedNode = null;
            chosenNode = null;
            if (node == null || e.InitialPressMouseButton != MouseButton.Left || !IsOverRow(node, e))
                return;

            RaiseEvent(new NodeChosenEventArgs(NodeChosenEvent, this, node));
        }

        // True when the pointer is on the row border of the node.
        private bool IsOverRow(TreeNode node, PointerEventArgs e) =>
            TreeRows.FindItem(treeView, node) is { } item
            && TreeRows.RowBorderOf(item) is { } border
            && new Rect(border.Bounds.Size).Contains(e.GetPosition(border));

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
            marker.Mark(null, null);

            if (DataContext is not ProjectTree tree || !tree.CanDropPaths(PathsOf(e)))
                return;

            e.DragEffects = DragDropEffects.Copy;
            var hoveredItem = TreeRows.ItemOf(e.Source);
            if (hoveredItem?.DataContext is TreeNode hovered && tree.IsPathTarget(hovered))
                marker.Mark(hoveredItem, DropPosition.Inside);
        }

        private void OnFilesDrop(DragEventArgs e)
        {
            e.Handled = true;
            marker.Mark(null, null);

            if (DataContext is ProjectTree tree)
                tree.DropPaths(TreeRows.ItemOf(e.Source)?.DataContext as TreeNode, PathsOf(e));
        }

        // The tree scrolls while a drag is near its top or bottom edge, and with the mouse wheel (see DragAutoScroller).
        // The scroller is made at the first drag and goes away when the view leaves the window.
        private DragAutoScroller? autoScroller;

        private ScrollViewer? ScrollerOfTree =>
            treeView.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        // True while the scroll of a drag runs.
        public bool IsAutoScrolling => autoScroller?.IsRunning ?? false;

        private void TrackDragPointer(DragEventArgs e)
        {
            autoScroller ??= new DragAutoScroller(
                (DataContext as ProjectTree)?.DragWheelSource ?? new NoDragWheelSource(), () => ScrollerOfTree, () => rowMetrics.RowPitch);
            autoScroller.Track(ScrollerOfTree is { } scroller ? e.GetPosition(scroller).Y : null);
        }

        public void ScrollByWheel(int steps) => autoScroller?.ScrollByWheel(steps);

        public double AutoScrollStep() => autoScroller?.AutoScrollStep() ?? 0;

        // A leave of one row comes with the enter of the next row. The enter keeps the scroll of the drag running.
        private void OnDragEnter(object? sender, DragEventArgs e) => TrackDragPointer(e);

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
            ShowBlockReason(source != null ? TreeRows.ItemOf(e.Source) : null);

            // A node of the tree always shows the move cursor. The cursor for a drop that is not possible is a red icon,
            // which does not fit the look of the app. The mark tells where a drop works: no mark, no move.
            if (source != null)
                e.DragEffects = DragDropEffects.Move;

            if (DataContext is not ProjectTree tree || source == null || item == null || target == null || position == null
                || !tree.CanDrop(source, target, position.Value))
            {
                marker.Mark(null, null);
                return;
            }

            marker.Mark(item, position);
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            // A leave also comes each time the pointer goes from one row to the next. The next enter or over cancels the stop.
            autoScroller?.StopDeferred();

            marker.Mark(null, null);
            ShowBlockReason(null);
        }

        // The row whose tooltip gives the reason of its block.
        private Control? blockTip;

        // A drag sends no pointer events to the rows, so the tooltip of a blocked row under the pointer opens from here. Null closes it.
        private void ShowBlockReason(TreeViewItem? hovered)
        {
            var row = hovered?.DataContext is TreeNode { IsDropBlocked: true } ? TreeRows.RowBorderOf(hovered) : null;
            if (ReferenceEquals(row, blockTip))
                return;

            if (blockTip != null)
                ToolTip.SetIsOpen(blockTip, false);

            blockTip = row;
            if (row != null)
                ToolTip.SetIsOpen(row, true);
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            try
            {
                autoScroller?.StopNow();

                if (e.Data.Contains(DataFormats.Files))
                {
                    OnFilesDrop(e);
                    return;
                }

                e.Handled = true;
                marker.Mark(null, null);
                ShowBlockReason(null);

                var (source, _, target, position) = Read(e);
                // A blocked row shows no mark, so it takes no drop.
                if (DataContext is not ProjectTree tree || source == null || target == null || position == null
                    || !tree.CanDrop(source, target, position.Value))
                    return;

                // A drop changes the number of rows above and under the view. The tree must not jump: the first row in view stays where it
                // is, and the tree does not scroll to the moved row. The move is done before the first await of DropAsync.
                var anchor = scrollAnchor.CaptureAnchor(source);

                Task drop;
                dropping = true;
                try
                {
                    drop = tree.DropAsync(source, target, position.Value);
                }
                finally
                {
                    dropping = false;
                }

                scrollAnchor.RestoreAnchor(anchor);
                Dispatcher.UIThread.Post(() => scrollAnchor.RestoreAnchor(anchor), DispatcherPriority.Loaded);
                await drop;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ProjectTreeView: the drop failed. {exception}");
            }
        }

        // The dragged node, the item that takes the drop, its node, and the place of the drop. The item that takes the drop
        // is the row under the pointer or one of its parents (see ProjectTree.Locate). It has no place when no drop is offered here.
        private (TreeNode? Source, TreeViewItem? Item, TreeNode? Target, DropPosition? Position) Read(DragEventArgs e)
        {
            var source = e.Data.Get(NodeFormat) as TreeNode;
            var hoveredItem = TreeRows.ItemOf(e.Source);
            if (source == null || hoveredItem?.DataContext is not TreeNode hovered || DataContext is not ProjectTree tree)
                return (source, null, null, null);

            var header = TreeRows.RowBorderOf(hoveredItem);
            var fraction = header == null || header.Bounds.Height <= 0
                ? 0.5
                : e.GetPosition(header).Y / header.Bounds.Height;

            // The pointer is in the space between two rows, not on a row: the drop stays where it was.
            if ((fraction < 0 || fraction > 1) && marker.Marked is { DataContext: TreeNode markedNode } marked && marker.MarkedPosition is { } markedPosition)
                return (source, marked, markedNode, markedPosition);

            if (tree.Locate(source, hovered, fraction) is not { } place)
                return (source, null, null, null);

            return (source, TreeRows.ItemOfNode(hoveredItem, place.Target), place.Target, place.Position);
        }
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
