// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
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
    // It can also go to a list of the same kind of another owner: an entity to another layer, a modifier to another entity,
    // a filter, a texture or a modulator to another owner of the same kind of list.
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
                if (target.Item == null || (source.Item is Layer && target.Item is Composition))
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
            if (target.IsOrIsUnder(source))
                return false;

            // A group takes an item of the same kind, inside it. An item of its own list goes to the end of the list. An item of another
            // list goes there too, unless that list has the item already.
            if (target.Item == null)
                return target.ChildManager != null
                    && KindOfItem(source) is { } kind
                    && KindOfGroup(target) == kind
                    && (ReferenceEquals(target.ChildManager, source.Owner) || !target.ChildManager.ManagerData.Items.Contains(source.Item!))
                    && Accepts(source.Item!, target.Group);

            if (target.Owner == null)
                return false;

            return ReferenceEquals(source.Owner, target.Owner)
                || (source.Item is Layer && (target.Item is Layer || target.Item is Composition))
                || CanMoveBetweenLists(source, target);
        }

        // The kinds of list that a layer has for its items. An item moves between layers only into a list of its own kind.
        private enum Slot
        {
            None,
            Entities,
            Cameras,
            Lights
        }

        private static Slot SlotOfList(Layer layer, PrefabManager? list) =>
            ReferenceEquals(list, layer.ModelEntityManager) ? Slot.Entities
            : ReferenceEquals(list, layer.CameraManager) ? Slot.Cameras
            : ReferenceEquals(list, layer.LightManager) ? Slot.Lights
            : Slot.None;

        // The kind of list that holds an item row of a layer. A layer, a composition and a group give None.
        private static Slot SlotOfItem(TreeNode item) =>
            item.Item != null && item.Parent?.Parent?.Item is Layer layer ? SlotOfList(layer, item.Owner) : Slot.None;

        // The kind of a list is the type of its group: entities, cameras, lights, textures, filters, modifiers or modulators.
        // An item moves between the lists of two owners only into a list of its own kind.
        private static System.Type? KindOfItem(TreeNode item) =>
            item.Item != null ? item.Parent?.Group?.GetType() : null;

        private static System.Type? KindOfGroup(TreeNode group) =>
            group.Item == null ? group.Group?.GetType() : null;

        // The list of the group can hold the item. A modifier goes only to an owner that can take it. The other kinds take any item of the kind.
        private static bool Accepts(IControl item, TreeGroup? group) =>
            group is not ModifiersGroup || AddableTypes.ForOwner(group.OwnerType).Contains(item.GetType());

        // The item is in a list of the same kind as the row of another owner, and that list does not have it yet.
        private static bool CanMoveBetweenLists(TreeNode source, TreeNode target) =>
            KindOfItem(source) is { } kind
            && kind == KindOfItem(target)
            && target.Owner != null
            && !ReferenceEquals(source.Owner, target.Owner)
            && !target.Owner.ManagerData.Items.Contains(source.Item!)
            && Accepts(source.Item!, target.Parent?.Group);

        private static TreeNode? CompositionOf(TreeNode node)
        {
            for (var current = node; current != null; current = current.Parent)
                if (current.Item is Composition)
                    return current;

            return null;
        }

        // True when a layer of the composition, other than the layer of the source, has the item in a list of the same kind.
        private static bool IsUsedByOtherLayers(TreeNode source)
        {
            if (CompositionOf(source) is not { Item: Composition composition } || source.Parent?.Parent?.Item is not Layer ownLayer)
                return false;

            var slot = SlotOfItem(source);
            if (slot == Slot.None)
                return false;

            return composition.LayerManager.ManagerData.Items.OfType<Layer>().Any(layer =>
                !ReferenceEquals(layer, ownLayer)
                && ListOf(layer, slot).ManagerData.Items.Contains(source.Item!));
        }

        private static PrefabManager ListOf(Layer layer, Slot slot) => slot switch
        {
            Slot.Cameras => layer.CameraManager,
            Slot.Lights => layer.LightManager,
            _ => layer.ModelEntityManager
        };

        // The last row that is shown in the block of a node: itself when it is collapsed or has no children.
        private static TreeNode LastVisible(TreeNode node) =>
            node.IsExpanded && node.Children.Count > 0 ? LastVisible(node.Children[^1]) : node;

        // True when the drop is possible by type and place. A move that shared items block is still possible here,
        // so the user can drop it and read the reason.
        public bool CanDrop(TreeNode source, TreeNode target, DropPosition position) =>
            Resolve(source, target, position) != null;

        // True when the dragged node can go inside the row: a group takes an item of its kind, and a composition takes a layer.
        // A drag shows these rows, since the drop on them has no bar between two rows to tell the place.
        public bool IsInsideTarget(TreeNode source, TreeNode target) =>
            source.Item != null && source.Owner != null
            && TakesDropOf(source, target)
            && Resolve(source, target, DropPosition.Inside) != null;

        // True when the dragged node can go before or after the row, in the list of the row. The place is shown by a bar between two rows.
        public bool IsNeighbor(TreeNode source, TreeNode target) =>
            source.Item != null && source.Owner != null
            && TakesDropOf(source, target)
            && (Resolve(source, target, DropPosition.Before) != null || Resolve(source, target, DropPosition.After) != null);

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

            // An item of a layer can go to a layer of another composition only if no other layer of its composition uses it.
            if (item is IPrefab prefab && source.Item is not Layer
                && !ReferenceEquals(CompositionOf(source), CompositionOf(target)) && IsUsedByOtherLayers(source))
                return new DropOutcome(false, new List<IPrefab> { prefab });

            var blocking = list.MoveItemToManager(item, owner, index);
            return new DropOutcome(blocking.Count == 0, blocking);
        }

        // The text for a move that shared items block.
        public string DescribeBlock(TreeNode source, TreeNode target, IReadOnlyList<IPrefab> blocking)
        {
            if (source.Item is not Layer)
            {
                var toLayer = (target.Item == null ? target.Parent : target.Parent?.Parent)?.Header ?? string.Empty;
                var fromComposition = CompositionOf(source)?.Header ?? string.Empty;

                return $"Cannot move \"{source.Header}\" to \"{toLayer}\".\n"
                    + $"This item is also used by other layers of \"{fromComposition}\".\n"
                    + "An item can belong to one composition only. "
                    + "Remove the item from the other layers first, or duplicate it, then move it.";
            }

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
                // A composition takes a layer inside it, and a group takes an item of its kind, at the end.
                if (target.ChildManager == null)
                    return null;

                var takes = target.Item is Composition ? source.Item is Layer : TakesDropOf(source, target);
                if (!takes)
                    return null;

                place = (target.ChildManager, target.ChildManager.ManagerData.Items.Count);
            }
            else
            {
                if (target.Item == null || target.Owner == null)
                    return null;

                var sameList = ReferenceEquals(source.Owner, target.Owner);
                var layerToLayerList = source.Item is Layer && target.Item is Layer;
                if (!sameList && !layerToLayerList && !CanMoveBetweenLists(source, target))
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
