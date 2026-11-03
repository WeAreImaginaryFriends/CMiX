// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
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

            ChildManager = childManager ?? (item is Composition composition ? composition.LayerManager : null);
            if (ChildManager != null)
            {
                foreach (var child in ChildManager.ManagerData.Items)
                    Children.Add(CreateItemNode(child));

                ChildManager.ManagerData.Items.CollectionChanged += OnItemsChanged;
                ChildManager.ManagerData.PropertyChanged += OnManagerDataChanged;
            }
            else if (item is Layer layer)
            {
                Children.Add(new TreeNode(tree, this, null, null, "Entities", layer.ModelEntityManager));
                Children.Add(new TreeNode(tree, this, null, null, "Cameras", layer.CameraManager));
                Children.Add(new TreeNode(tree, this, null, null, "Lights", layer.LightManager));
            }
        }

        public TreeNode? Parent { get; }
        public string IconKey { get; }
        public IControl? Item { get; }
        public PrefabManager? Owner { get; }
        public PrefabManager? ChildManager { get; }
        public ObservableCollection<TreeNode> Children { get; } = new();

        [ObservableProperty]
        private string header;

        [ObservableProperty]
        private bool isExpanded = true;

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
            null => header switch { "Cameras" => "Camera", "Lights" => "MeshLight", _ => "Geometry" },
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
                    RemoveChildren(0, Children.Count);
                    InsertChildren(0, ChildManager!.ManagerData.Items);
                    break;
            }
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
