// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Modifiers;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using CMiX.Core.Rendering.Lights;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // A node of the project tree: a composition, a layer, an item of a layer, or a group of items.
    // Item is null for a group. Owner is the list that holds the Item.
    // ChildManager is the list whose items are the children. The children follow that list.
    public sealed partial class TreeNode : ObservableObject, IDisposable
    {
        private readonly ProjectTree tree;

        internal TreeNode(ProjectTree tree, TreeNode? parent, PrefabManager? owner, IControl? item, string header, PrefabManager? childManager, TreeGroup? group = null)
        {
            this.tree = tree;
            Children.CollectionChanged += (_, _) => RaiseKindCountsChanged();
            Parent = parent;
            Owner = owner;
            Item = item;
            Group = group;
            this.header = group?.Header ?? header;
            IconKey = group?.IconKey ?? IconFor(item);

            if (item is IPrefab prefab)
            {
                this.header = prefab.PrefabService.Name.Value;
                prefab.PrefabService.Name.PropertyChanged += OnNameChanged;
            }

            Category = item != null ? TreeCategories.Of(item) : group?.Category;
            ComposableDepth = parent == null ? 0 : parent.ComposableDepth + (parent.Category == TreeCategory.Composable ? 1 : 0);
            KindDepth = parent == null ? 0 : parent.KindDepth + (parent.Category != null && Category != null && parent.Category != Category ? 1 : 0);

            ChildManager = childManager ?? (item is Composition composition ? composition.LayerManager : null);
            if (ChildManager != null)
            {
                foreach (var child in ChildManager.ManagerData.Items)
                    Children.Add(CreateItemNode(child));

                ChildManager.ManagerData.Items.CollectionChanged += OnItemsChanged;
                ChildManager.ManagerData.PropertyChanged += OnManagerDataChanged;
            }

            // The groups come after the items of the node (the layers of a composition), so the place of an item in its list is its
            // place in the children.
            if (item != null)
                AddGroups(item);
        }

        // The lists that an item holds, as groups (see TreeGroup.GroupsOf). A group row has no item. The inspector shows the panel of the group.
        private void AddGroups(IControl item)
        {
            foreach (var group in TreeGroup.GroupsOf(item))
                Children.Add(new TreeNode(tree, this, null, null, group.Header, group.Manager, group));
        }

        public TreeNode? Parent { get; }
        public string IconKey { get; }
        public IControl? Item { get; }
        public TreeGroup? Group { get; }

        // The kind of the row: its color in the tree. Null for a row that the tree does not know.
        public TreeCategory? Category { get; }

        // The number of composables around the row. A composable has one more color for each level.
        public int ComposableDepth { get; }

        // The number of changes of kind from the top of the tree to the row. A group and its items have the same one, and what a row holds
        // of another kind has one more. A row is darker for each one (see CategoryPalette).
        public int KindDepth { get; }

        // What the inspector shows for this row: the item, or the group of items of a layer.
        public object? InspectorContent => (object?)Item ?? Group;
        public PrefabManager? Owner { get; }
        public PrefabManager? ChildManager { get; }
        public ObservableCollection<TreeNode> Children { get; } = new();

        // The kinds of what the row holds, in the order of the rows under it, each with its number: an item under the row counts for
        // its own kind, and a group under the row counts for the items in it. A collapsed row shows these.
        public IReadOnlyList<KindCount> KindCounts
        {
            get
            {
                var counts = new Dictionary<TreeCategory, int>();
                foreach (var child in Children)
                {
                    if (child.Category is not { } category)
                        continue;

                    var number = child.Item != null ? 1 : child.Children.Count;
                    if (number > 0)
                        counts[category] = counts.GetValueOrDefault(category) + number;
                }

                return counts.Select(count => new KindCount(count.Key, count.Value)).ToList();
            }
        }

        public bool HasKindCounts => KindCounts.Count > 0;

        // The item that has the mask toggle: a composition or a layer. The other rows have none. The toggle column shows the toggle for it.
        public IControl? MaskOwner => Item is Composition or Layer ? Item : null;

        // A collapsed row shows what it holds. An open row shows it in the rows under it.
        public bool ShowsKindCounts => HasKindCounts && !IsExpanded;

        // The rows under a group change what its parent holds, so the parent tells its own change too.
        private void RaiseKindCountsChanged()
        {
            OnPropertyChanged(nameof(KindCounts));
            OnPropertyChanged(nameof(HasKindCounts));
            OnPropertyChanged(nameof(ShowsKindCounts));
            if (Item == null)
                Parent?.RaiseKindCountsChanged();
        }

        [ObservableProperty]
        private string header;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowsKindCounts))]
        [NotifyPropertyChangedFor(nameof(IsDropHint))]
        [NotifyPropertyChangedFor(nameof(IsDimmed))]
        private bool isExpanded = true;

        // The state of the row during a drag of a node (see ProjectTree.BeginDrag). The rows that take the node inside are candidates: they
        // are lit. The rows that the node can go next to are neighbors: they stay as they are, and the bar shows the place. A collapsed row
        // that has such a row under it gives a hint, so the user knows to open it. A row that takes the node by its kind but refuses
        // it by a rule is blocked: it keeps its fill, shows a frame and gives the reason. The other rows are dimmed.
        public bool IsDragActive { get; private set; }
        public bool IsDragSource { get; private set; }
        public bool IsDropCandidate { get; private set; }
        public bool IsDropNeighbor { get; private set; }
        public bool HasDropTargetBelow { get; private set; }

        // The reason why the row refuses the dragged node (see TreeDropService.BlockReason), or null.
        public string? DropBlockReason { get; private set; }
        public bool IsDropBlocked => DropBlockReason != null;
        public bool IsDropHint => IsDragActive && HasDropTargetBelow && !IsDropCandidate && !IsDropNeighbor && !IsDropBlocked && !IsExpanded;
        public bool IsDimmed => IsDragActive && !IsDragSource && !IsDropCandidate && !IsDropNeighbor && !IsDropBlocked && !IsDropHint;

        internal void SetDragState(bool active, bool isSource, bool isCandidate, bool isNeighbor, bool hasTargetBelow, string? blockReason = null)
        {
            IsDragActive = active;
            IsDragSource = isSource;
            IsDropCandidate = isCandidate;
            IsDropNeighbor = isNeighbor;
            HasDropTargetBelow = hasTargetBelow;
            DropBlockReason = blockReason;
            OnPropertyChanged(nameof(IsDragActive));
            OnPropertyChanged(nameof(IsDragSource));
            OnPropertyChanged(nameof(IsDropCandidate));
            OnPropertyChanged(nameof(IsDropNeighbor));
            OnPropertyChanged(nameof(HasDropTargetBelow));
            OnPropertyChanged(nameof(DropBlockReason));
            OnPropertyChanged(nameof(IsDropBlocked));
            OnPropertyChanged(nameof(IsDropHint));
            OnPropertyChanged(nameof(IsDimmed));
        }

        public bool IsOrIsUnder(TreeNode node)
        {
            for (var current = this; current != null; current = current.Parent)
                if (ReferenceEquals(current, node))
                    return true;
            return false;
        }

        // The icon of an item row, or of a row that is neither an item nor a group.
        private static string IconFor(IControl? item) => item switch
        {
            Composition => "Composition",
            Layer => "LayerScene",
            FullscreenQuad => "FullscreenQuad",
            TextEntity => "TextIcon",
            Camera => "Camera",
            LightEntity => "MeshLight",
            IModifier => "Modifier",
            IModulator => "Modulator",
            ITextureFilter => "Filter",
            ITextureSource => "Texture",
            _ => "Geometry"
        };

        private TreeNode CreateItemNode(IControl item) =>
            new(tree, this, ChildManager, item, string.Empty, null);

        private void OnNameChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" && Item is IPrefab prefab)
                Header = prefab.PrefabService.Name.Value;
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // A new item starts collapsed: the user opens it.
                    InsertChildren(e.NewStartingIndex, e.NewItems!.Cast<IControl>(), expanded: false);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveChildren(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Move:
                    Children.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveChildren(e.OldStartingIndex, e.OldItems!.Count);
                    InsertChildren(e.NewStartingIndex, e.NewItems!.Cast<IControl>());
                    break;
                default:
                    // The groups after the items stay.
                    RemoveChildren(0, Children.Count(child => child.Item != null));
                    InsertChildren(0, ChildManager!.ManagerData.Items);
                    break;
            }

            tree.ListChanged();
        }

        private void InsertChildren(int index, System.Collections.Generic.IEnumerable<IControl> items, bool expanded = true)
        {
            foreach (var item in items)
            {
                var node = CreateItemNode(item);
                node.IsExpanded = expanded;
                Children.Insert(index++, node);
            }
        }

        private void RemoveChildren(int index, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var node = Children[index];
                tree.NodeRemoved(node);
                Children.RemoveAt(index);
                node.Dispose();
            }
        }

        private void OnManagerDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ManagerData.SelectedItem))
                return;

            var selected = ChildManager!.ManagerData.SelectedItem;
            var node = Children.FirstOrDefault(child => ReferenceEquals(child.Item, selected));
            if (node != null)
                tree.SelectFromModel(node);
        }

        public void Dispose()
        {
            if (Item is IPrefab prefab)
                prefab.PrefabService.Name.PropertyChanged -= OnNameChanged;

            if (ChildManager != null)
            {
                ChildManager.ManagerData.Items.CollectionChanged -= OnItemsChanged;
                ChildManager.ManagerData.PropertyChanged -= OnManagerDataChanged;
            }

            foreach (var child in Children)
                child.Dispose();
        }
    }
}
