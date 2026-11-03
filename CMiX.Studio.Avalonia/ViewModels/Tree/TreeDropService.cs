// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    public enum DropPosition
    {
        Before,
        After,
        Inside
    }

    // The row that takes a drop, and where the drop goes in relation to it.
    public sealed record DropPlace(TreeNode Target, DropPosition Position);

    // What a drop did. Blocking lists the items that stopped a move of a layer.
    public sealed record DropOutcome(bool Moved, IReadOnlyList<IPrefab> Blocking)
    {
        public static DropOutcome None { get; } = new(false, new List<IPrefab>());
    }

    // The rules for a drag in the project tree.
    // An item moves inside its own list, so a composition goes with compositions and an entity with entities.
    // A layer can also go to the layer list of another composition, if no item of it is shared with a layer that stays.
    public sealed class TreeDropService
    {
        // Where a drop lands when the pointer is over a row. The fraction is the height of the pointer on that row,
        // from 0 at the top to 1 at the bottom. The result is null when no drop is offered at this place.
        //
        // The row that takes the drop is the hovered row, or the first parent row that can take the dragged node.
        // A row with rows under it is one block, so the drop is offered only at the two ends of the block:
        // before it, in the top half of its own row, and after it, in the bottom half of the last visible row of the block.
        // A row without rows under it has both halves. A layer over a composition goes inside it, wherever the pointer is on the row.
        public DropPlace? Locate(TreeNode source, TreeNode hovered, double fraction)
        {
            if (source.Item == null || source.Owner == null || hovered.IsOrIsUnder(source))
                return null;

            var target = hovered;
            while (target != null && !TakesDropOf(source, target))
                target = target.Parent;

            if (target == null)
                return null;

            if (ReferenceEquals(hovered, target))
            {
                if (source.Item is Layer && target.Item is Composition)
                    return new DropPlace(target, DropPosition.Inside);

                if (fraction < 0.5)
                    return new DropPlace(target, DropPosition.Before);

                return ReferenceEquals(LastVisible(target), target) ? new DropPlace(target, DropPosition.After) : null;
            }

            return ReferenceEquals(LastVisible(target), hovered) && fraction >= 0.5 ? new DropPlace(target, DropPosition.After) : null;
        }

        // True when the node is a row that the dragged node can go before, after, or (a layer on a composition) inside.
        private static bool TakesDropOf(TreeNode source, TreeNode target)
        {
            if (target.Item == null || target.Owner == null || target.IsOrIsUnder(source))
                return false;

            return ReferenceEquals(source.Owner, target.Owner)
                || (source.Item is Layer && (target.Item is Layer || target.Item is Composition));
        }

        // The last row that is shown in the block of a node: itself when it is collapsed or has no children.
        private static TreeNode LastVisible(TreeNode node) =>
            node.IsExpanded && node.Children.Count > 0 ? LastVisible(node.Children[^1]) : node;

        // True when the drop is possible by type and place. A move that shared items block is still possible here,
        // so the user can drop it and read the reason.
        public bool CanDrop(TreeNode source, TreeNode target, DropPosition position) =>
            Resolve(source, target, position) != null;

        public DropOutcome Drop(TreeNode source, TreeNode target, DropPosition position)
        {
            var resolved = Resolve(source, target, position);
            if (resolved == null)
                return DropOutcome.None;

            var (list, index) = resolved.Value;
            var item = source.Item!;
            var owner = source.Owner!;
            if (ReferenceEquals(list, owner))
            {
                list.MoveItem(list.ManagerData.Items.IndexOf(item), NewIndex(source, list, index));
                return new DropOutcome(true, new List<IPrefab>());
            }

            var blocking = list.MoveItemToManager(item, owner, index);
            return new DropOutcome(blocking.Count == 0, blocking);
        }

        // The text for a move that shared items block.
        public string DescribeBlock(TreeNode source, TreeNode target, IReadOnlyList<IPrefab> blocking)
        {
            var layer = source.Header;
            var from = source.Parent?.Header ?? string.Empty;
            var to = (target.Item is Composition ? target : target.Parent)?.Header ?? string.Empty;
            var names = string.Join(", ", blocking.Select(item => item.PrefabService.Name.Value));

            return $"Cannot move \"{layer}\" to \"{to}\".\n"
                + $"This layer shares items with other layers of \"{from}\": {names}.\n"
                + "An item can belong to one composition only. "
                + "Remove the item from the other layers first, or duplicate it, then move the layer.";
        }

        // The list that takes the item, and the index in it. Null when the drop is not possible.
        private static (PrefabManager List, int Index)? Resolve(TreeNode source, TreeNode target, DropPosition position)
        {
            // A group and the top of the tree cannot be dragged.
            if (source.Item == null || source.Owner == null)
                return null;

            if (target.IsOrIsUnder(source))
                return null;

            (PrefabManager List, int Index)? place;
            if (position == DropPosition.Inside)
            {
                // Only a composition takes a layer inside it, at the end.
                if (target.Item is not Composition || source.Item is not Layer || target.ChildManager == null)
                    return null;

                place = (target.ChildManager, target.ChildManager.ManagerData.Items.Count);
            }
            else
            {
                if (target.Item == null || target.Owner == null)
                    return null;

                var sameList = ReferenceEquals(source.Owner, target.Owner);
                var layerToLayerList = source.Item is Layer && target.Item is Layer;
                if (!sameList && !layerToLayerList)
                    return null;

                var index = target.Owner.ManagerData.Items.IndexOf(target.Item) + (position == DropPosition.After ? 1 : 0);
                place = (target.Owner, index);
            }

            // A drop that leaves the item where it is changes nothing, so it is not a drop.
            if (ReferenceEquals(place.Value.List, source.Owner) && NewIndex(source, place.Value.List, place.Value.Index) == place.Value.List.ManagerData.Items.IndexOf(source.Item))
                return null;

            return place;
        }

        // The index of the item in its own list after it moves to the place with that index, counted before the move.
        private static int NewIndex(TreeNode source, PrefabManager list, int index)
        {
            var oldIndex = list.ManagerData.Items.IndexOf(source.Item!);
            return index > oldIndex ? index - 1 : index;
        }
    }
}
