// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // The compositions of the project with their layers, and the entities, cameras and lights of each layer.
    // The tree follows the lists, also when a peer changes them.
    // A selected node selects its whole chain in the lists, as one undo step.
    public sealed class ProjectTree : ObservableObject, IDisposable
    {
        private readonly UndoManager undoManager;
        private readonly IMoveBlockedNotice? notice;
        private readonly TreeDropService dropService = new();
        private readonly PrefabManager compositions;
        private readonly TreeNode root;
        private TreeNode? selectedNode;
        private bool selecting;

        public ProjectTree(Project project, UndoManager undoManager, IMoveBlockedNotice? notice = null)
        {
            this.undoManager = undoManager;
            this.notice = notice;
            compositions = project.CompositionManager;
            root = new TreeNode(this, null, null, null, "Project", compositions);

            AddItemCommand = new RelayCommand(AddItem, () => GetAddChoices(selectedNode).Count > 0);
            DuplicateItemCommand = new RelayCommand(
                () => { if (SelectedPlace() is var (list, item, _)) list.DuplicateItem(item); },
                () => SelectedPlace() != null);
            MoveItemUpCommand = new RelayCommand(
                () => { if (SelectedPlace() is var (list, item, index)) list.MoveItemTo(item, index - 1); },
                () => SelectedPlace() is var (_, _, index) && index > 0);
            MoveItemDownCommand = new RelayCommand(
                () => { if (SelectedPlace() is var (list, item, index)) list.MoveItemTo(item, index + 1); },
                () => SelectedPlace() is var (list, _, index) && index < list.ManagerData.Items.Count - 1);
            RemoveItemCommand = new RelayCommand(
                () => { if (SelectedPlace() is var (list, item, _)) list.RemoveItem(item); },
                () => SelectedPlace() != null);
        }

        public ObservableCollection<TreeNode> Roots => root.Children;

        // The commands of the toolbar. They act on the selected node, and they are disabled when it has nothing to do.
        // AddItemCommand adds when there is only one thing to add. With several, the toolbar asks (see GetAddChoices).
        public IRelayCommand AddItemCommand { get; }
        public IRelayCommand DuplicateItemCommand { get; }
        public IRelayCommand MoveItemUpCommand { get; }
        public IRelayCommand MoveItemDownCommand { get; }
        public IRelayCommand RemoveItemCommand { get; }

        // What can be added under a node. With no node, a composition is added to the project.
        // A row with children is a submenu. The same list feeds the menu of a row and the toolbar.
        public IReadOnlyList<TreeAdd> GetAdds(TreeNode? node)
        {
            if (node == null)
                return new[] { AddOf("row.add-composition", "Add Composition", compositions, typeof(Composition)) };

            if (node.Item is Composition && node.ChildManager is { } layers)
                return new[] { AddOf("row.add-layer", "Add Layer", layers, typeof(Layer)) };

            // A layer takes an item of any kind: the entities, then the camera and the light.
            if (node.Item is Layer own)
                return new[]
                {
                    new TreeAdd("row.add", "Add", Children: new[]
                    {
                        AddOf("row.add-entity", "Entity", own.ModelEntityManager, typeof(Entity)),
                        AddOf("row.add-fullscreen-quad", "Fullscreen Quad", own.ModelEntityManager, typeof(FullscreenQuad)),
                        AddOf("row.add-text-entity", "Text", own.ModelEntityManager, typeof(TextEntity)),
                        AddOf("row.add-camera", "Camera", own.CameraManager, typeof(Camera)),
                        AddOf("row.add-light", "Light", own.LightManager, typeof(LightEntity))
                    })
                };

            // A group of a layer takes the kind of item of its list.
            if (node.Item != null || node.ChildManager is not { } list || node.Parent?.Item is not Layer layer)
                return Array.Empty<TreeAdd>();

            if (ReferenceEquals(list, layer.ModelEntityManager))
                return new[]
                {
                    AddOf("row.add-entity", "Add Entity", list, typeof(Entity)),
                    AddOf("row.add-fullscreen-quad", "Add Fullscreen Quad", list, typeof(FullscreenQuad)),
                    AddOf("row.add-text-entity", "Add Text", list, typeof(TextEntity))
                };

            if (ReferenceEquals(list, layer.CameraManager))
                return new[] { AddOf("row.add-camera", "Add Camera", list, typeof(Camera)) };

            if (ReferenceEquals(list, layer.LightManager))
                return new[] { AddOf("row.add-light", "Add Light", list, typeof(LightEntity)) };

            return Array.Empty<TreeAdd>();
        }

        // The rows that add one thing: the children of a submenu, or the row itself.
        public IReadOnlyList<TreeAdd> GetAddChoices(TreeNode? node) =>
            GetAdds(node).SelectMany(add => add.Children ?? new[] { add }).ToList();

        private static TreeAdd AddOf(string id, string header, PrefabManager list, Type type) =>
            new(id, header, () => list.AddItem(type));

        private void AddItem()
        {
            var choices = GetAddChoices(selectedNode);
            if (choices.Count == 1)
                choices[0].Execute?.Invoke();
        }

        // The selected node as an item of a list: the list, the item and its place in the list.
        // A group, or no node, is not an item.
        private (PrefabManager List, IControl Item, int Index)? SelectedPlace()
        {
            if (selectedNode is not { Owner: { } list, Item: { } item })
                return null;

            var index = list.ManagerData.Items.IndexOf(item);
            return index < 0 ? null : (list, item, index);
        }

        private void RefreshCommands()
        {
            AddItemCommand?.NotifyCanExecuteChanged();
            DuplicateItemCommand?.NotifyCanExecuteChanged();
            MoveItemUpCommand?.NotifyCanExecuteChanged();
            MoveItemDownCommand?.NotifyCanExecuteChanged();
            RemoveItemCommand?.NotifyCanExecuteChanged();
        }

        // A list of the tree changed: a place in a list, and so the commands, may have changed.
        internal void ListChanged() => RefreshCommands();

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SelectedNode))
                RefreshCommands();
        }

        public bool CanDrop(TreeNode source, TreeNode target, DropPosition position) =>
            dropService.CanDrop(source, target, position);

        public DropPlace? Locate(TreeNode source, TreeNode hovered, double fraction) =>
            dropService.Locate(source, hovered, fraction);

        // Drops the node. A move that shared items block leaves everything as it is and tells the user why.
        public async Task DropAsync(TreeNode source, TreeNode target, DropPosition position)
        {
            var outcome = dropService.Drop(source, target, position);
            if (outcome.Moved || outcome.Blocking.Count == 0 || notice == null)
                return;

            await notice.ShowAsync(dropService.DescribeBlock(source, target, outcome.Blocking));
        }

        public TreeNode? SelectedNode
        {
            get => selectedNode;
            set
            {
                if (!SetProperty(ref selectedNode, value) || value == null || selecting)
                    return;

                SelectChain(value);
            }
        }

        // Selects the item of every node from the composition down to the node.
        private void SelectChain(TreeNode node)
        {
            var chain = new List<TreeNode>();
            for (var current = node; current != null; current = current.Parent)
                chain.Insert(0, current);

            selecting = true;
            undoManager.BeginCapture();
            try
            {
                foreach (var step in chain)
                    if (step.Owner != null && step.Item != null)
                        step.Owner.SelectedItem = step.Item;
            }
            finally
            {
                undoManager.EndCapture();
                selecting = false;
            }
        }

        // A list changed its selection. The tree follows without writing back to the lists.
        internal void SelectFromModel(TreeNode node)
        {
            if (selecting)
                return;

            SetProperty(ref selectedNode, node, nameof(SelectedNode));
        }

        // A node leaves the tree. A selection inside it moves to the parent.
        internal void NodeRemoved(TreeNode node)
        {
            if (selectedNode == null || !selectedNode.IsOrIsUnder(node))
                return;

            SetProperty(ref selectedNode, ReferenceEquals(node.Parent, root) ? null : node.Parent, nameof(SelectedNode));
        }

        public void Dispose() => root.Dispose();
    }
}
