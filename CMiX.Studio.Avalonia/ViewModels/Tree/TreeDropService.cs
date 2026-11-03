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
        // The place of a drop on a row. The fraction is the height of the pointer on the row, from 0 at the top to 1 at the bottom.
        // A layer over a composition goes inside it, wherever the pointer is on the row.
        // Every other drop goes before the row in its top half, and after the row in its bottom half.
        public DropPosition PositionFor(TreeNode source, TreeNode target, double fraction)
        {
            if (source.Item is Layer && target.Item is Composition)
                return DropPosition.Inside;

            return fraction < 0.5 ? DropPosition.Before : DropPosition.After;
        }

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
                var oldIndex = list.ManagerData.Items.IndexOf(item);
                var newIndex = index > oldIndex ? index - 1 : index;
                if (newIndex == oldIndex)
                    return DropOutcome.None;

                list.MoveItem(oldIndex, newIndex);
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

            if (position == DropPosition.Inside)
            {
                // Only a composition takes a layer inside it, at the end.
                if (target.Item is Composition && source.Item is Layer && target.ChildManager != null)
                    return (target.ChildManager, target.ChildManager.ManagerData.Items.Count);

                return null;
            }

            if (target.Item == null || target.Owner == null)
                return null;

            var sameList = ReferenceEquals(source.Owner, target.Owner);
            var layerToLayerList = source.Item is Layer && target.Item is Layer;
            if (!sameList && !layerToLayerList)
                return null;

            var index = target.Owner.ManagerData.Items.IndexOf(target.Item) + (position == DropPosition.After ? 1 : 0);
            return (target.Owner, index);
        }
    }
}
