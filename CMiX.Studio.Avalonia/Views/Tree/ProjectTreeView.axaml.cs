// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CMiX.Core.Prefabs.Managers;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;
using CameraModel = CMiX.Core.Rendering.Cameras.Camera;
using CompositionModel = CMiX.Core.Compositing.Composition;
using EntityModel = CMiX.Core.Compositing.Entity;
using FullscreenQuadModel = CMiX.Core.Compositing.FullscreenQuad;
using LayerModel = CMiX.Core.Compositing.Layer;
using LightModel = CMiX.Core.Rendering.Lights.LightEntity;
using TextEntityModel = CMiX.Core.Compositing.TextEntity;

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
        private const string DropInside = ":dropinside";

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

            var item = ItemOf(e.Source);
            var menu = item?.DataContext is TreeNode node ? CreateMenu(item, node) : null;
            treeView.ContextMenu = menu;
            if (menu == null)
                return;

            menu.Open(treeView);
            e.Handled = true;
        }

        private static ContextMenu? CreateMenu(TreeViewItem row, TreeNode node)
        {
            var adds = AddsFor(node);
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

        // What the menu of a row can add: a layer to a composition, an item of any kind to a layer,
        // and an item to a group of a layer.
        private static IReadOnlyList<RowMenuAdd> AddsFor(TreeNode node)
        {
            if (node.Item is CompositionModel && node.ChildManager is { } layers)
                return new[] { Add("row.add-layer", "Add Layer", layers, typeof(LayerModel)) };

            // The panel of a layer: the entities, then the camera and the light.
            if (node.Item is LayerModel own)
                return new[]
                {
                    new RowMenuAdd("row.add", "Add", Children: new[]
                    {
                        Add("row.add-entity", "Entity", own.ModelEntityManager, typeof(EntityModel)),
                        Add("row.add-fullscreen-quad", "Fullscreen Quad", own.ModelEntityManager, typeof(FullscreenQuadModel)),
                        Add("row.add-text-entity", "Text", own.ModelEntityManager, typeof(TextEntityModel)),
                        Add("row.add-camera", "Camera", own.CameraManager, typeof(CameraModel)),
                        Add("row.add-light", "Light", own.LightManager, typeof(LightModel))
                    })
                };

            if (node.Item != null || node.ChildManager is not { } list || node.Parent?.Item is not LayerModel layer)
                return Array.Empty<RowMenuAdd>();

            if (ReferenceEquals(list, layer.ModelEntityManager))
                return new[]
                {
                    Add("row.add-entity", "Add Entity", list, typeof(EntityModel)),
                    Add("row.add-fullscreen-quad", "Add Fullscreen Quad", list, typeof(FullscreenQuadModel)),
                    Add("row.add-text-entity", "Add Text", list, typeof(TextEntityModel))
                };

            if (ReferenceEquals(list, layer.CameraManager))
                return new[] { Add("row.add-camera", "Add Camera", list, typeof(CameraModel)) };

            if (ReferenceEquals(list, layer.LightManager))
                return new[] { Add("row.add-light", "Add Light", list, typeof(LightModel)) };

            return Array.Empty<RowMenuAdd>();
        }

        private static RowMenuAdd Add(string id, string header, PrefabManager list, Type type) =>
            new(id, header, () => list.AddItem(type));

        private static TreeViewItem? ItemOf(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is TreeViewItem item)
                    return item;

            return null;
        }

        private static bool IsEditor(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
                if (visual is ToggleButton or TextBox)
                    return true;

            return false;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            pressedNode = null;
            if (!e.GetCurrentPoint(treeView).Properties.IsLeftButtonPressed || IsEditor(e.Source))
                return;

            pressedNode = ItemOf(e.Source)?.DataContext as TreeNode;
            pressPoint = e.GetPosition(treeView);
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
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            Mark(null, null);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => pressedNode = null;

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;

            var (source, item, target, position) = Read(e);

            // A node of the tree always shows the move cursor. The cursor for a drop that is not possible is a red icon,
            // which does not fit the look of the app. The mark tells where a drop works: no mark, no move.
            if (source != null)
                e.DragEffects = DragDropEffects.Move;

            if (DataContext is not ProjectTree tree || source == null || item == null || target == null || !tree.CanDrop(source, target, position))
            {
                Mark(null, null);
                return;
            }

            Mark(item, position);
        }

        private void OnDragLeave(object? sender, DragEventArgs e) => Mark(null, null);

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            Mark(null, null);

            var (source, _, target, position) = Read(e);
            if (DataContext is not ProjectTree tree || source == null || target == null)
                return;

            await tree.DropAsync(source, target, position);
        }

        // The dragged node, the row under the pointer, its node, and the place in the row.
        private (TreeNode? Source, TreeViewItem? Item, TreeNode? Target, DropPosition Position) Read(DragEventArgs e)
        {
            var source = e.Data.Get(NodeFormat) as TreeNode;
            var item = ItemOf(e.Source);
            var target = item?.DataContext as TreeNode;
            if (item == null || target == null)
                return (source, item, target, DropPosition.After);

            var header = item.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Name == "RowBorder");
            var fraction = header == null || header.Bounds.Height <= 0
                ? 0.5
                : e.GetPosition(header).Y / header.Bounds.Height;

            // The pointer is in the space between two rows, not on a row: the drop stays where it was.
            if ((fraction < 0 || fraction > 1) && marked != null && marked.DataContext is TreeNode markedNode && markedPosition != null)
                return (source, marked, markedNode, markedPosition.Value);

            if (DataContext is not ProjectTree tree || source == null)
                return (source, item, target, DropPosition.After);

            return (source, item, target, tree.PositionFor(source, target, fraction));
        }

        private void Mark(TreeViewItem? item, DropPosition? position)
        {
            if (marked != null)
            {
                var old = (IPseudoClasses)marked.Classes;
                old.Set(DropBefore, false);
                old.Set(DropAfter, false);
                old.Set(DropInside, false);
            }

            marked = item;
            markedPosition = position;
            if (item == null || position == null)
                return;

            ((IPseudoClasses)item.Classes).Set(
                position == DropPosition.Before ? DropBefore : position == DropPosition.After ? DropAfter : DropInside,
                true);
        }
    }
}
