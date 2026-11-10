// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;
using CMiX.UI.Controls.Boolean;
using CMiX.UI.Controls.String;
using CMiX.UI.Popups;
using CMiX.UI.Behaviors;
using CompositionModel = CMiX.Core.Compositing.Composition;
using LayerModel = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Views
{
    // The tree of the project. A row is dragged with the pointer to reorder it in its list (see TreeRowDrag).
    // A layer can also go to another composition. TreeDropService holds the rules. Files from the file manager drop on the tree too.
    public partial class ProjectTreeView : UserControl
    {
        // The space between two rows at a screen of 100% (see TreeRowMetrics).
        public const double RowGap = TreeRowMetrics.RowGap;

        private readonly TreeRowMetrics rowMetrics;
        private readonly TreeRowIndex rowIndex;
        private readonly TreeDropMarker marker;
        private readonly TreeScrollAnchor scrollAnchor;
        private readonly TreeRowDrag rowDrag;
        private readonly TreeTooltips tooltips;

        // The tooltips of the rows. A test reads it.
        internal TreeTooltips Tooltips => tooltips;

        // The panel of the Add row of the menus of the rows. One panel serves every menu.
        private readonly AddPanel menuAddPanel = new();

        // The node of the row that the left button pressed and that can be dragged. A drag clears it.
        private TreeNode? pressedNode;

        // The panel of the Add row of the menus. A test reads it.
        internal AddPanel MenuAddPanel => menuAddPanel;

        // The item of each node. A test reads it.
        internal TreeRowIndex RowIndex => rowIndex;

        // The mark of the drop. A test reads it.
        internal TreeDropMarker Marker => marker;

        // The drag of a row. A test reads it.
        internal TreeRowDrag RowDrag => rowDrag;

        public ProjectTreeView()
        {
            InitializeComponent();

            // The bands start at the top of the rows, which is the panel of the items of the tree.
            treeView.Loaded += (_, _) => stripes.Rows = treeView.ItemsPanelRoot;

            rowMetrics = new TreeRowMetrics(treeView, Resources, () => ScrollerOfTree);
            rowIndex = new TreeRowIndex(treeView);
            marker = new TreeDropMarker(rowIndex);
            scrollAnchor = new TreeScrollAnchor(treeView, rowIndex, () => ScrollerOfTree);
            rowDrag = new TreeRowDrag(treeView, () => DataContext as ProjectTree, marker, () => AutoScroller, scrollAnchor, () => ScrollerOfTree);

            DragDrop.SetAllowDrop(treeView, true);
            treeView.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
            treeView.AddHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
            treeView.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
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
            AddHandler(PointerMovedEvent, OnPointerTracked, RoutingStrategies.Tunnel, handledEventsToo: true);
            KeyBindingTextGuard.Install(this);
            tooltips = new TreeTooltips(treeView);
        }

        // The tree does not scroll by itself when the user clicks a row: a click on a row that is at the edge of the view would scroll to
        // show the whole block of the row, and the second click of a double click would land on another row. A row that a program or a
        // key selects is brought into view here (RevealRow), and only its own row, not the rows under it.
        private ProjectTree? watched;
        private bool attached;

        // The row border that RevealRow brings into view.
        private Visual? revealTarget;

        // A row asks to be brought into view when it gets the focus or the selection. That request scrolls to the whole block of the
        // row, so it stops here, under the scroller of the tree. Only the request of RevealRow goes through.
        private void OnRequestBringIntoView(object? sender, RequestBringIntoViewEventArgs e)
        {
            if (!ReferenceEquals(e.TargetObject, revealTarget))
                e.Handled = true;
        }

        // Escape ends a drag of a row without a drop.
        // Avalonia runs the key bindings of the view (the gestures of the map) before this handler. The navigation keys move the soft
        // selection only (see ProjectTree.SoftSelectedNode). The tree view does not see them, so they select nothing. A navigation key
        // with a modifier does nothing. A key in a name box stays in the name box.
        private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && rowDrag.IsDragging)
            {
                rowDrag.Cancel();
                e.Handled = true;
                return;
            }

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
            if (rowIndex.FindItem(node) is not { } item)
                return;

            item.Focus(NavigationMethod.Tab);
            RevealRow(node);
        }

        // The last place of the pointer over the tree, in the coordinates of the tree view. Tree.Add uses it to find the row.
        private Point lastPointer;

        private void OnPointerTracked(object? sender, PointerEventArgs e) => lastPointer = e.GetPosition(treeView);

        // Tree.Add: the Add panel of the selected row opens with its search field under the pointer, and the title Add. With nothing selected it holds what the
        // project takes, a composition. Nothing is added until the user picks an entry of the panel.
        private void OnAddRequested(object? sender, EventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return;

            // The panel opens with the middle of its search field under the pointer.
            menuAddPanel.OpenAtPoint(treeView, lastPointer, tree.GetAddGroups(tree.SelectedNode), AddTitle);
        }

        // Tree.Menu: the menu of the selected row opens at the pointer, as a right click opens it. With nothing selected, it is the menu
        // of the project. A selected row that is not shown (its parent is collapsed) opens no menu.
        private void OnMenuRequested(object? sender, EventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return;

            if (tree.SelectedNode is not { } node)
                OpenMenu(null);
            else if (rowIndex.FindItem(node) is { } item)
                OpenMenu(item);
        }

        // The title of the Add panel when no row opens it.
        private const string AddTitle = "Add";

        // Starts the rename of the row of the node: the name box of the row goes to edit mode.
        private void OnRenameRequested(object? sender, TreeNode node)
        {
            if (rowIndex.FindItem(node) is not { } item || TreeRows.RowBorderOf(item) is not { } row)
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
            watched.AddRequested += OnAddRequested;
            watched.MenuRequested += OnMenuRequested;
            watched.IsPointerOverTree = () => IsPointerOver;
            watched.IsTextBoxFocused = () => IsInTextBox(TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement());
            watched.ToggleDragRow = rowDrag.ToggleHovered;
            watched.IsTreeFocused = () => IsKeyboardFocusWithin;
            watched.TreeFocusRequested += OnTreeFocusRequested;
            watched.CancelDrag = rowDrag.Cancel;
        }

        private void Unwatch()
        {
            if (watched == null)
                return;

            watched.PropertyChanged -= OnWatchedPropertyChanged;
            watched.RenameRequested -= OnRenameRequested;
            watched.AddRequested -= OnAddRequested;
            watched.MenuRequested -= OnMenuRequested;
            watched.IsPointerOverTree = () => false;
            watched.IsTextBoxFocused = () => false;
            watched.ToggleDragRow = () => { };
            watched.IsTreeFocused = () => false;
            watched.TreeFocusRequested -= OnTreeFocusRequested;
            watched.CancelDrag = () => { };
        }

        // The focus goes to a row: the selected row, else the row of the arrow keys, else the first row (see ProjectTree.FocusTree).
        private void OnTreeFocusRequested(object? sender, EventArgs e)
        {
            if (watched is not { } tree)
                return;

            var node = tree.SelectedNode ?? tree.SoftSelectedNode ?? tree.VisibleNodes().FirstOrDefault();
            if (node != null && rowIndex.FindItem(node) is { } item && item.Focus(NavigationMethod.Tab))
                return;

            treeView.Focus();
        }

        private void OnWatchedPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProjectTree.SelectedNode) || rowDrag.IsDropping || watched?.SelectedNode is not { } node
                || watched.Origin is not (SelectionOrigin.Program or SelectionOrigin.Peer))
                return;

            Dispatcher.UIThread.Post(() => RevealRow(node), DispatcherPriority.Loaded);
        }

        private void RevealRow(TreeNode node)
        {
            if (rowIndex.FindItem(node) is not { } item || TreeRows.RowBorderOf(item) is not { } border)
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
            rowDrag.Cancel();
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
            if (OpenMenu(e.Source))
                e.Handled = true;
        }

        // Opens the menu of the row of the source. On the space under the rows, it is the menu of the project, which has the one entry
        // that the project takes, a composition. Both menus add through the Add panel. Returns false when there is no menu.
        private bool OpenMenu(object? source)
        {
            treeView.ContextMenu?.Close();

            var item = TreeRows.ItemOf(source);
            var menu = item?.DataContext is TreeNode node && DataContext is ProjectTree tree ? CreateMenu(item, node, tree)
                : item == null && DataContext is ProjectTree project ? CreateSpaceMenu(project)
                : null;
            treeView.ContextMenu = menu;
            if (menu == null)
                return false;

            menu.Open(treeView);
            return true;
        }

        // The menu of the empty space of the tree: its Add row opens the panel of the project.
        private ContextMenu CreateSpaceMenu(ProjectTree tree) =>
            PrefabRowMenu.Create(treeView, RowMenuKind.Group, () => null, Array.Empty<RowMenuAdd>(), () => "Project",
                addRow: new AddMenuRow(menuAddPanel, () => tree.GetAddGroups(null)),
                gestureOf: command => tree.Shortcuts[command]);

        // The menu of a row. Its Add row opens the Add panel beside the menu, with what the row takes. The menu opens with the Add row
        // under the pointer, on every row, until the user picks another row of a menu: then that row is under the pointer (see MenuAnchor).
        private ContextMenu? CreateMenu(TreeViewItem row, TreeNode node, ProjectTree tree)
        {
            var kind = node.Item == null ? RowMenuKind.Group
                : node.Item is CompositionModel or LayerModel ? RowMenuKind.Composable
                : RowMenuKind.Slot;


            return PrefabRowMenu.Create(
                row,
                kind,
                () => node.Owner != null && node.Item != null ? (node.Owner, node.Item) : null,
                Array.Empty<RowMenuAdd>(),
                () => node.Header,
                addRow: new AddMenuRow(menuAddPanel, () => tree.GetAddGroups(node)),
                gestureOf: command => tree.Shortcuts[command]);
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
            if (rowDrag.IsDragging || !e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed || IsEditor(e.Source))
                return;

            var node = TreeRows.ItemOf(e.Source)?.DataContext as TreeNode;
            // The toggles at the right of a row never start a drag, not even the space around them.
            pressedNode = node != null && !IsInToggles(e.Source) && DataContext is ProjectTree tree && tree.CanDrag(node) ? node : null;
            chosenNode = node;
            rowDrag.Press(pressedNode, e.GetPosition(treeView));
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed)
                return;

            rowDrag.Move(e.GetPosition(treeView), e);
            if (rowDrag.IsDragging)
            {
                pressedNode = null;
                chosenNode = null;
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var node = chosenNode;
            pressedNode = null;
            chosenNode = null;
            rowDrag.Release(e.GetPosition(treeView));
            if (node == null || e.InitialPressMouseButton != MouseButton.Left || !IsOverRow(node, e))
                return;

            RaiseEvent(new NodeChosenEventArgs(NodeChosenEvent, this, node));
        }

        // The capture goes when the window loses the pointer or the tree leaves the window: the drag ends without a drop.
        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => rowDrag.Cancel();

        // The wheel during a drag of a row scrolls the tree by rows (see DragAutoScroller). The scroll viewer does not see it.
        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (!rowDrag.IsDragging)
                return;

            var steps = (int)Math.Round(e.Delta.Y);
            rowDrag.Wheel(steps != 0 ? steps : Math.Sign(e.Delta.Y));
            e.Handled = true;
        }

        // True when the pointer is on the row border of the node.
        private bool IsOverRow(TreeNode node, PointerEventArgs e) =>
            rowIndex.FindItem(node) is { } item
            && TreeRows.RowBorderOf(item) is { } border
            && new Rect(border.Bounds.Size).Contains(e.GetPosition(border));

        // The tree scrolls while a drag is near its top or bottom edge (see DragAutoScroller).
        // The scroller is made at the first drag and goes away when the view leaves the window.
        private DragAutoScroller? autoScroller;

        private DragAutoScroller AutoScroller =>
            autoScroller ??= new DragAutoScroller(
                () => ScrollerOfTree, () => rowMetrics.RowPitch);

        private ScrollViewer? ScrollerOfTree =>
            treeView.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        // True while the scroll of a drag runs.
        public bool IsAutoScrolling => autoScroller?.IsRunning ?? false;

        public void ScrollByWheel(int steps) => autoScroller?.ScrollByWheel(steps);

        public double AutoScrollStep() => autoScroller?.AutoScrollStep() ?? 0;

        // The files and folders from the file manager come as a drag of the system. The tree is their drop target.
        // Any other drag of the system does nothing here: a row of the tree is dragged with the pointer (see TreeRowDrag).
        private static bool IsFileDrag(DragEventArgs e) => e.Data.Contains(DataFormats.Files);

        // The paths of the current file drag, classified once. Null when no file drag is over the tree.
        private DropFiles? dropFiles;

        // The record of the drag. A drag with other data is classified again. DragOver reads no disk.
        private DropFiles? FilesOf(DragEventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return null;

            if (dropFiles == null || !ReferenceEquals(dropFiles.Data, e.Data))
                dropFiles = DropFiles.Classify(e.Data, FileDropMode.FilesAndFolders, tree.AcceptsPath);
            return dropFiles;
        }

        private void TrackFilesPointer(DragEventArgs e) =>
            AutoScroller.Track(ScrollerOfTree is { } scroller ? e.GetPosition(scroller).Y : null);

        // A leave of one row comes with the enter of the next row. The next enter or over starts the timer again.
        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (!IsFileDrag(e))
                return;

            FilesOf(e);
            TrackFilesPointer(e);
        }

        // Files over the tree: the row under the pointer looks hovered when that row takes the files itself (see
        // ProjectTree.IsPathTarget). Over a child of a layer, or over the space under the rows, nothing is marked.
        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (!IsFileDrag(e))
                return;

            TrackFilesPointer(e);
            e.Handled = true;
            e.DragEffects = DragDropEffects.None;
            marker.Mark(null, null);

            if (DataContext is not ProjectTree tree || FilesOf(e) is not { IsAccepted: true })
                return;

            e.DragEffects = DragDropEffects.Copy;
            var hoveredItem = TreeRows.ItemOf(e.Source);
            if (hoveredItem?.DataContext is TreeNode hovered && tree.IsPathTarget(hovered))
                marker.Mark(hoveredItem, DropPosition.Inside);
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            autoScroller?.StopNow();
            marker.Mark(null, null);
            // A leave of a row inside the tree keeps the record.
            if (DropFiles.LeavesControl(treeView, e))
                dropFiles = null;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (!IsFileDrag(e))
                return;

            autoScroller?.StopNow();
            e.Handled = true;
            marker.Mark(null, null);
            var files = FilesOf(e);
            dropFiles = null;
            if (DataContext is ProjectTree tree && files != null)
                tree.DropPaths(TreeRows.ItemOf(e.Source)?.DataContext as TreeNode, files.Accepted);
        }
    }
}
