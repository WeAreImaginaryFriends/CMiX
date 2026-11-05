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
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Texturing;
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

        internal TreeNode(ProjectTree tree, TreeNode? parent, PrefabManager? owner, IControl? item, string header, PrefabManager? childManager)
        {
            this.tree = tree;
            Children.CollectionChanged += (_, _) => RaiseKindCountsChanged();
            Parent = parent;
            Owner = owner;
            Item = item;
            this.header = header;
            IconKey = IconFor(item, header);

            if (item is IPrefab prefab)
            {
                this.header = prefab.PrefabService.Name.Value;
                prefab.PrefabService.Name.PropertyChanged += OnNameChanged;
            }

            // A group row has no item. The inspector shows the panel of the group.
            if (item == null)
                Group = TreeGroup.For(parent?.Item, childManager);

            Category = item != null ? TreeCategories.Of(item) : Group != null ? TreeCategories.Of(Group) : null;
            ComposableDepth = parent == null ? 0 : parent.ComposableDepth + (parent.Category == TreeCategory.Composable ? 1 : 0);

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

        // The lists that an item holds, as groups: the lists of a layer, then the textures, the filters and the modifiers.
        private void AddGroups(IControl item)
        {
            if (item is Layer layer)
            {
                AddGroup("Entities", layer.ModelEntityManager);
                AddGroup("Cameras", layer.CameraManager);
                AddGroup("Lights", layer.LightManager);
            }

            if (item is ITexturable textured && textured.Texture?.DiffuseTexture?.TextureManager is { } textures)
                AddGroup("Textures", textures);

            if (item is ITextureModifiable filtered && filtered.TextureModifierManager is { } filters)
                AddGroup("Filters", filters);

            if (item is IModifiable modified && modified.ModifierManager is { } modifiers)
                AddGroup("Modifiers", modifiers);

            if (item is IModulatable modulated && modulated.ModulatorManager is { } modulators)
                AddGroup("Modulators", modulators);
        }

        private void AddGroup(string header, PrefabManager list) =>
            Children.Add(new TreeNode(tree, this, null, null, header, list));

        public TreeNode? Parent { get; }
        public string IconKey { get; }
        public IControl? Item { get; }
        public TreeGroup? Group { get; }

        // The kind of the row: its color in the tree. Null for a row that the tree does not know.
        public TreeCategory? Category { get; }

        // The number of composables around the row. A composable has one more color for each level.
        public int ComposableDepth { get; }

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

        // The state of the row during a drag of a node (see ProjectTree.BeginDrag). The rows that can take the node are candidates.
        // A collapsed row that has a candidate under it gives a hint, so the user knows to open it. The other rows are dimmed.
        public bool IsDragActive { get; private set; }
        public bool IsDragSource { get; private set; }
        public bool IsDropCandidate { get; private set; }
        public bool HasDropCandidateBelow { get; private set; }
        public bool IsDropHint => IsDragActive && HasDropCandidateBelow && !IsDropCandidate && !IsExpanded;
        public bool IsDimmed => IsDragActive && !IsDragSource && !IsDropCandidate && !IsDropHint;

        internal void SetDragState(bool active, bool isSource, bool isCandidate, bool hasCandidateBelow)
        {
            IsDragActive = active;
            IsDragSource = isSource;
            IsDropCandidate = isCandidate;
            HasDropCandidateBelow = hasCandidateBelow;
            OnPropertyChanged(nameof(IsDragActive));
            OnPropertyChanged(nameof(IsDragSource));
            OnPropertyChanged(nameof(IsDropCandidate));
            OnPropertyChanged(nameof(HasDropCandidateBelow));
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

        private static string IconFor(IControl? item, string header) => item switch
        {
            Composition => "Composition",
            Layer => "LayerScene",
            FullscreenQuad => "FullscreenQuad",
            TextEntity => "TextIcon",
            Camera => "Camera",
            LightEntity => "MeshLight",
            null => header switch
            {
                "Cameras" => "Camera",
                "Lights" => "MeshLight",
                "Textures" => "Texture",
                "Filters" => "Filter",
                "Modifiers" => "Modifier",
                "Modulators" => "Modulator",
                _ => "Geometry"
            },
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
                    InsertChildren(e.NewStartingIndex, e.NewItems!.Cast<IControl>());
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

        private void InsertChildren(int index, System.Collections.Generic.IEnumerable<IControl> items)
        {
            foreach (var item in items)
                Children.Insert(index++, CreateItemNode(item));
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
