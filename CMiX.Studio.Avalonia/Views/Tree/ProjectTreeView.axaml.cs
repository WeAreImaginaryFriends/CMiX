// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using HanumanInstitute.MvvmDialogs;
using CompositionModel = CMiX.Core.Compositing.Composition;

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

        private readonly TreeDropService dropService = new();
        private TreeNode? pressedNode;
        private Point pressPoint;
        private TreeViewItem? marked;

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
        }

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
            if (source == null || item == null || target == null || !dropService.CanDrop(source, target, position))
            {
                Mark(null, null);
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            Mark(item, position);
        }

        private void OnDragLeave(object? sender, DragEventArgs e) => Mark(null, null);

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            Mark(null, null);

            var (source, _, target, position) = Read(e);
            if (source == null || target == null)
                return;

            var outcome = dropService.Drop(source, target, position);
            if (outcome.Moved || outcome.Blocking.Count == 0)
                return;

            var dialogService = App.DialogService;
            if (dialogService != null && DataContext is ProjectTree tree)
                await dialogService.ShowMessageBoxAsync(tree, dropService.DescribeBlock(source, target, outcome.Blocking), "Move Layer");
        }

        // The dragged node, the row under the pointer, its node, and the place in the row.
        private (TreeNode? Source, TreeViewItem? Item, TreeNode? Target, DropPosition Position) Read(DragEventArgs e)
        {
            var source = e.Data.Get(NodeFormat) as TreeNode;
            var item = ItemOf(e.Source);
            var target = item?.DataContext as TreeNode;
            if (item == null || target == null)
                return (source, item, target, DropPosition.After);

            var header = item.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Name == "PART_LayoutRoot");
            var fraction = header == null || header.Bounds.Height <= 0
                ? 0.5
                : e.GetPosition(header).Y / header.Bounds.Height;

            // A composition has a middle part that takes a layer inside it. Other rows split in two.
            var position = target.Item is CompositionModel
                ? fraction < 0.25 ? DropPosition.Before : fraction > 0.75 ? DropPosition.After : DropPosition.Inside
                : fraction < 0.5 ? DropPosition.Before : DropPosition.After;

            return (source, item, target, position);
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
            if (item == null || position == null)
                return;

            ((IPseudoClasses)item.Classes).Set(
                position == DropPosition.Before ? DropBefore : position == DropPosition.After ? DropAfter : DropInside,
                true);
        }
    }
}
