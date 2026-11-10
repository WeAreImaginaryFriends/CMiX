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
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Rendering.Cameras;
using CMiX.Core.Rendering.Lights;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
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
        private readonly PathDropService pathDrop;
        private readonly PrefabManager compositions;
        private readonly Project project;
        private readonly TreeNode root;
        private TreeNode? selectedNode;
        private IReadOnlyList<TreeAdd> selectedAdds;
        private bool selecting;

        public ProjectTree(Project project, UndoManager undoManager, IMoveBlockedNotice? notice = null,
            ShortcutMap? shortcuts = null, PathDropService? pathDrop = null)
        {
            this.undoManager = undoManager;
            this.notice = notice;
            this.pathDrop = pathDrop ?? new PathDropService();
            Shortcuts = shortcuts ?? new ShortcutMap();
            this.project = project;
            compositions = project.CompositionManager;
            // The one row under the root is the group of the compositions. The compositions are under it.
            root = new TreeNode(this, null, null, null, "Project", null);
            root.AddGroups(project);

            selectedAdds = GetAddChoices(null);
            AddItemCommand = new RelayCommand<TreeAdd?>(add => add?.Execute?.Invoke(), add => add == null ? HasAdds : add.Execute != null);
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
            AddUnderPointerCommand = new RelayCommand(
                () => AddRequested?.Invoke(this, EventArgs.Empty),
                () => IsPointerOverTree());
            ToggleExpandUnderPointerCommand = new RelayCommand(
                () => { if (ExpandTarget() is { } node) node.IsExpanded = !node.IsExpanded; },
                () => IsPointerOverTree() && !IsTextBoxFocused() && ExpandTarget() != null);
            OpenMenuUnderPointerCommand = new RelayCommand(
                () => MenuRequested?.Invoke(this, EventArgs.Empty),
                () => IsPointerOverTree() && !IsTextBoxFocused());
            RenameSelectedCommand = new RelayCommand(
                () => { if (selectedNode is { Item: not null } node) RenameRequested?.Invoke(this, node); },
                () => selectedNode?.Item != null);
            OpenSelectedCommand = new RelayCommand(
                () => Select(softSelectedNode, SelectionOrigin.Keyboard),
                () => softSelectedNode != null);
        }

        // True while the project loads. A row made then starts collapsed. A row made by the user, or by a peer later, starts expanded.
        internal bool IsLoading => project.IsLoading;

        // The gestures of the key bindings of the tree view (the names Tree.*).
        public ShortcutMap Shortcuts { get; }

        // Opens the Add panel of the selected row at the pointer (Tree.Add). With nothing selected, the panel offers a composition. The panel
        // adds nothing at once. The command runs only while the pointer is over the
        // tree, so the key works from anywhere over the tree and does nothing over the rest of the window.
        public IRelayCommand AddUnderPointerCommand { get; }

        // The view tells if the pointer is over the tree.
        public Func<bool> IsPointerOverTree { get; set; } = () => false;

        // The view handles this request: it opens the Add panel.
        public event EventHandler? AddRequested;

        // Opens the menu of the selected row at the pointer (Tree.Menu), or the menu of the project with nothing selected. The command
        // runs only while the pointer is over the tree, and not while a text box has the focus: the key is a plain letter.
        public IRelayCommand OpenMenuUnderPointerCommand { get; }

        // Expands or collapses the row that the arrow keys are on, else the selected row (Tree.Expand). A row with no children does not
        // change. The command runs only while the pointer is over the tree, and not while a text box has the focus: the key is a plain letter.
        public IRelayCommand ToggleExpandUnderPointerCommand { get; }

        private TreeNode? ExpandTarget() => (softSelectedNode ?? selectedNode) is { Children.Count: > 0 } node ? node : null;

        // The view tells if a text box has the focus.
        public Func<bool> IsTextBoxFocused { get; set; } = () => false;

        // The view handles this request: it opens the menu.
        public event EventHandler? MenuRequested;

        // Starts the rename of the selected row. The view sets the name box of the row to edit mode (see RenameRequested).
        public IRelayCommand RenameSelectedCommand { get; }

        // Selects the soft selected node and its chain in the lists.
        public IRelayCommand OpenSelectedCommand { get; }

        // The view handles this request: it starts the rename of the row of the node.
        public event EventHandler<TreeNode>? RenameRequested;

        private TreeNode? softSelectedNode;

        // The row that the arrow keys move to. It does not select the chain: no undo step and no wire message.
        // Enter (OpenSelectedCommand) or a click selects the chain. A change of SelectedNode moves it to the selected node.
        public TreeNode? SoftSelectedNode
        {
            get => softSelectedNode;
            set
            {
                if (SetProperty(ref softSelectedNode, value))
                    OpenSelectedCommand?.NotifyCanExecuteChanged();
            }
        }

        // The nodes that show a row, from the top: a child shows when all its parents are expanded.
        public IReadOnlyList<TreeNode> VisibleNodes()
        {
            var nodes = new List<TreeNode>();
            void Add(IEnumerable<TreeNode> children)
            {
                foreach (var child in children)
                {
                    nodes.Add(child);
                    if (child.IsExpanded)
                        Add(child.Children);
                }
            }

            Add(Roots);
            return nodes;
        }

        // Moves the soft selection by a number of visible rows. It stops at the first and the last row.
        public void MoveSoftSelection(int rows)
        {
            var nodes = VisibleNodes();
            if (nodes.Count == 0)
                return;

            var index = softSelectedNode == null ? -1 : IndexOf(nodes, softSelectedNode);
            var next = index < 0 ? (rows >= 0 ? 0 : nodes.Count - 1) : Math.Clamp(index + rows, 0, nodes.Count - 1);
            SoftSelectedNode = nodes[next];
        }

        public void MoveSoftSelectionToEnd(bool last)
        {
            var nodes = VisibleNodes();
            if (nodes.Count > 0)
                SoftSelectedNode = last ? nodes[^1] : nodes[0];
        }

        // Left: an expanded row collapses, else the soft selection goes to the parent row.
        public void MoveSoftSelectionOut()
        {
            if (softSelectedNode is not { } node)
                return;

            if (node.IsExpanded && node.Children.Count > 0)
                node.IsExpanded = false;
            else if (node.Parent is { } parent && !ReferenceEquals(parent, root))
                SoftSelectedNode = parent;
        }

        // Right: a collapsed row expands, else the soft selection goes to the first child row.
        public void MoveSoftSelectionIn()
        {
            if (softSelectedNode is not { Children.Count: > 0 } node)
                return;

            if (!node.IsExpanded)
                node.IsExpanded = true;
            else
                SoftSelectedNode = node.Children[0];
        }

        private static int IndexOf(IReadOnlyList<TreeNode> nodes, TreeNode node)
        {
            for (var i = 0; i < nodes.Count; i++)
                if (ReferenceEquals(nodes[i], node))
                    return i;

            return -1;
        }

        public ObservableCollection<TreeNode> Roots => root.Children;

        // The commands of the toolbar. They act on the selected node, and they are disabled when it has nothing to do.
        // AddItemCommand runs the add that it gets. The toolbar picks the add (see SelectedAdds). With no add, it tells only
        // whether the selected node has something to add.
        public IRelayCommand<TreeAdd?> AddItemCommand { get; }
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

            if (node.Item is Composition composition)
                return new[] { AddOf("row.add-layer", "Add Layer", composition.LayerManager, typeof(Layer)) }.Concat(HeldAdds(node.Item)).ToList();

            // A layer takes an item of any kind: the entities, then the camera and the light.
            if (node.Item is Layer own)
                return new[]
                {
                    new TreeAdd("row.add", "Add", Children: TreeGroup.GroupsOf(own).OfType<LayerGroup>()
                        .SelectMany(group => group.AddableTypes.Select(type => LayerAddOf(group, type, string.Empty)))
                        .ToList())
                }.Concat(HeldAdds(own)).ToList();

            // Any other item takes the modifiers, the filters and the textures that it can hold.
            if (node.Item != null)
                return HeldAdds(node.Item);

            // A group of a layer takes the kind of item of its list.
            if (node.Group is LayerGroup layerGroup)
                return layerGroup.AddableTypes.Select(type => LayerAddOf(layerGroup, type, "Add ")).ToList();

            // The group of the layers of a composition takes a layer, and the group of the compositions takes a composition.
            if (node.Group is LayersGroup layersGroup)
                return new[] { AddOf("row.add-layer", "Add Layer", layersGroup.Manager, typeof(Layer)) };

            if (node.Group is CompositionsGroup)
                return GetAdds(null);

            // A group of modifiers, filters or textures takes one of the types that its owner can hold.
            if (node.Group is { } held)
                return AddsOfTypes(held);

            return Array.Empty<TreeAdd>();
        }

        // The rows that add one thing, for the Add button: the children of a submenu, or the row itself.
        // The composition is under no row, so it can be added whatever is selected. It comes last, after what the row takes.
        // The menu of a row does not offer it. The space menu and the toolbar do.
        public IReadOnlyList<TreeAdd> GetAddChoices(TreeNode? node)
        {
            // The submenu of the layer is flat. The submenus of textures, filters and modifiers stay submenus: they are long.
            var choices = GetAdds(node).SelectMany(add => add.Id == "row.add" ? add.Children! : new[] { add }).ToList();
            if (node != null)
                choices.AddRange(GetAdds(null));

            return choices;
        }

        // The types that a layer takes as content, in the order of the Add panel.
        private static readonly Type[] ContentTypes = { typeof(Entity), typeof(FullscreenQuad), typeof(TextEntity), typeof(Camera), typeof(LightEntity) };

        // What the Add panel shows for a node. Every node gets the same groups in the same order: Layer, Content, Texture, Filter, Modifier,
        // Modulator and Project. An entry that the node cannot take is not left out. It is there, not enabled, and the panel greys it.
        // So the panel looks the same whatever is selected, and the user finds an entry where it was. The headers of the entries have no
        // "Add". An entry has the icon of its type when the type has one (see TreeIcons).
        // What a node can take comes from what it holds (see AvailableAdds). The composition can be added under any node.
        public IReadOnlyList<TreeAddGroup> GetAddGroups(TreeNode? node)
        {
            var available = AvailableAdds(node);

            TreeAdd Entry(string id, string header, Type type, TreeCategory category) =>
                available.TryGetValue(id, out var add)
                    ? add with { Header = header, Category = category }
                    : new TreeAdd(id, header, Category: category, Enabled: false);

            TreeAddGroup Held(TreeCategory category, IReadOnlyList<Type> types)
            {
                var name = TreeCategories.Label(category, 1);
                var entries = types.Select(type => Entry("row.add-" + type.Name, AddableTypes.LabelOf(type), type, category)).ToList();
                return new TreeAddGroup("group.add-" + name.ToLowerInvariant(), name, category, entries);
            }

            var content = ContentTypes.Select(type =>
            {
                var (id, label) = LayerEntryOf(type);
                return Entry(id, label, type, ContentCategoryOf(type));
            }).ToList();

            return new[]
            {
                new TreeAddGroup("group.layer", "Layer", TreeCategory.Composable,
                    new[] { Entry("row.add-layer", "Layer", typeof(Layer), TreeCategory.Composable) }),
                new TreeAddGroup("group.content", "Content", null, content),
                Held(TreeCategory.Texture, AddableTypes.ForInterface(typeof(ITextureSource))),
                Held(TreeCategory.Filter, AddableTypes.ForInterface(typeof(ITextureFilter))),
                Held(TreeCategory.Modifier, AddableTypes.AllModifiers()),
                Held(TreeCategory.Modulator, AddableTypes.ForInterface(typeof(IModulator))),
                new TreeAddGroup("group.project", "Project", TreeCategory.Composable,
                    new[] { Entry("row.add-composition", "Composition", typeof(Composition), TreeCategory.Composable) }),
            };
        }

        // The entries that a node can take, by id, with the action that adds each one. They come from the lists that the node holds: the
        // layers of a composition, the entities, cameras and lights of a layer, and the textures, filters, modifiers and modulators of
        // any item. A row that is a group of a layer or of an item takes the kind of its own list. The composition goes under any node.
        private Dictionary<string, TreeAdd> AvailableAdds(TreeNode? node)
        {
            var available = new Dictionary<string, TreeAdd>();
            void Take(TreeAdd add) => available.TryAdd(add.Id, add);
            void TakeHeld(TreeGroup group)
            {
                foreach (var type in group.AddableTypes)
                    Take(AddOf("row.add-" + type.Name, AddableTypes.LabelOf(type), group.Manager, type));
            }

            if (node?.Item is Composition composition)
            {
                Take(AddOf("row.add-layer", "Layer", composition.LayerManager, typeof(Layer)));
                foreach (var group in TreeGroup.GroupsOf(node.Item).Where(group => group is not (LayerGroup or StructureGroup)))
                    TakeHeld(group);
            }
            else if (node?.Item is Layer own)
            {
                foreach (var group in TreeGroup.GroupsOf(own))
                {
                    if (group is LayerGroup layerGroup)
                        TakeLayerItems(layerGroup, Take);
                    else
                        TakeHeld(group);
                }
            }
            else if (node?.Item != null)
            {
                foreach (var group in TreeGroup.GroupsOf(node.Item).Where(group => group is not (LayerGroup or StructureGroup)))
                    TakeHeld(group);
            }
            else if (node?.Group is LayersGroup layersGroup)
            {
                Take(AddOf("row.add-layer", "Layer", layersGroup.Manager, typeof(Layer)));
            }
            else if (node?.Group is CompositionsGroup)
            {
                // The composition is taken for every node, below.
            }
            else if (node?.Group is LayerGroup layerGroup)
            {
                TakeLayerItems(layerGroup, Take);
            }
            else if (node?.Group is { } held)
            {
                TakeHeld(held);
            }

            Take(AddOf("row.add-composition", "Composition", compositions, typeof(Composition)));
            return available;
        }

        private static void TakeLayerItems(LayerGroup group, Action<TreeAdd> take)
        {
            foreach (var type in group.AddableTypes)
                take(LayerAddOf(group, type, string.Empty));
        }

        // The category of a type that a layer takes as content.
        private static TreeCategory ContentCategoryOf(Type type) =>
            type == typeof(Camera) ? TreeCategory.Camera : type == typeof(LightEntity) ? TreeCategory.Light : TreeCategory.Entity;

        // The id and the label of a type that a layer takes as content.
        private static (string Id, string Label) LayerEntryOf(Type type) =>
            type == typeof(FullscreenQuad) ? ("row.add-fullscreen-quad", "Fullscreen Quad")
                : type == typeof(TextEntity) ? ("row.add-text-entity", "Text")
                : type == typeof(Camera) ? ("row.add-camera", "Camera")
                : type == typeof(LightEntity) ? ("row.add-light", "Light")
                : ("row.add-entity", "Entity");

        // The add choices of the selected node. The tree makes them again when the selection changes.
        public IReadOnlyList<TreeAdd> SelectedAdds => selectedAdds;

        // True when the selected node has something to add. It enables the Add button.
        public bool HasAdds => selectedAdds.Count > 0;

        // The submenus of an item for what it can hold: textures, filters, modifiers and modulators. A submenu is there only when the item
        // can hold the kind.
        private static IReadOnlyList<TreeAdd> HeldAdds(IControl item) =>
            TreeGroup.GroupsOf(item)
                .Where(group => group is not (LayerGroup or StructureGroup))
                .Select(group =>
                {
                    var name = TreeCategories.Label(group.Category, 1);
                    return new TreeAdd("row.add-" + name.ToLowerInvariant(), "Add " + name, Children: AddsOfTypes(group), Secondary: true);
                })
                .ToList();

        private static IReadOnlyList<TreeAdd> AddsOfTypes(TreeGroup group) =>
            group.AddableTypes
                .Select(type => AddOf("row.add-" + type.Name, AddableTypes.LabelOf(type), group.Manager, type))
                .ToList();

        // An add row for an item of a layer. The prefix is "Add " in the menu of a group, and empty in the submenu of the layer.
        private static TreeAdd LayerAddOf(LayerGroup group, Type type, string prefix)
        {
            var (id, label) = LayerEntryOf(type);
            return AddOf(id, prefix + label, group.Manager, type);
        }

        private static TreeAdd AddOf(string id, string header, PrefabManager list, Type type) =>
            new(id, header, () => list.AddItem(type));

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
            RenameSelectedCommand?.NotifyCanExecuteChanged();
        }

        // A list of the tree changed: a place in a list, and so the commands, may have changed.
        internal void ListChanged() => RefreshCommands();

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SelectedNode))
            {
                var hadAdds = HasAdds;
                selectedAdds = GetAddChoices(selectedNode);
                OnPropertyChanged(nameof(SelectedAdds));
                if (hadAdds != HasAdds)
                    OnPropertyChanged(nameof(HasAdds));

                SoftSelectedNode = selectedNode;
                RefreshCommands();
            }
        }

        // A group (Entities, Cameras, Lights) has a fixed place in its layer. Only an item can be dragged.
        public bool CanDrag(TreeNode node) => node.Item != null && node.Owner != null;

        // A row that the drag marks as blocked takes no drop: the marker shows nothing over it.
        public bool CanDrop(TreeNode source, TreeNode target, DropPosition position) =>
            !target.IsDropBlocked && dropService.CanDrop(source, target, position);

        // A node is being dragged: the rows that take it inside are candidates, the rows next to which it can go are neighbors, a collapsed row
        // with such a row under it gives a hint, a row that refuses the node by a rule is blocked, and the other rows are dimmed
        // (see TreeNode.IsDropCandidate). The rules are the ones of the drop itself.
        public bool IsDragging { get; private set; }

        // The rows that BeginDrag marked. EndDrag clears only these.
        private readonly List<TreeNode> dragMarked = new();

        public void BeginDrag(TreeNode source)
        {
            IsDragging = true;
            dragMarked.Clear();
            MarkDrag(root, source);
        }

        public void EndDrag()
        {
            IsDragging = false;
            foreach (var node in dragMarked)
                node.SetDragState(false, false, false, false, false);

            dragMarked.Clear();
        }

        // Sets the drag state of a row and of the rows under it. It tells whether a row that takes the node is at or under the row.
        private bool MarkDrag(TreeNode node, TreeNode source)
        {
            var below = false;
            foreach (var child in node.Children)
                below |= MarkDrag(child, source);

            var candidate = node != root && dropService.IsInsideTarget(source, node);
            var neighbor = node != root && dropService.IsNeighbor(source, node);
            var reason = node != root && (candidate || neighbor || dropService.IsTargetByKind(source, node)) ? dropService.BlockReason(source, node) : null;
            if (reason != null)
                candidate = neighbor = false;

            node.SetDragState(true, ReferenceEquals(node, source), candidate, neighbor, below, reason);
            dragMarked.Add(node);
            return below || candidate || neighbor;
        }

        public DropPlace? Locate(TreeNode source, TreeNode hovered, double fraction) =>
            dropService.Locate(source, hovered, fraction);

        // Drops the node. A move that shared items block leaves everything as it is and tells the user why.
        public async Task DropAsync(TreeNode source, TreeNode target, DropPosition position)
        {
            // The move and the selection of the moved item are one undo step.
            DropOutcome outcome;
            var inside = TreeDropService.InsideOf(source, target);
            undoManager.BeginCapture();
            try
            {
                outcome = dropService.Drop(source, target, position);
                if (outcome.Moved)
                {
                    // The rows that hold the item open, so the user sees the result.
                    // A layer row passes the item to its group, so the group opens too.
                    var owner = position == DropPosition.Inside ? inside : target.Parent;
                    for (var holder = owner; holder != null; holder = holder.Parent)
                        holder.IsExpanded = true;

                    // Search under the holder only. A shared item is also under other layers.
                    if (FindNode(owner ?? root, source.Item!) is { } moved)
                        Select(moved, SelectionOrigin.Program);
                }
            }
            finally
            {
                undoManager.EndCapture();
            }

            if (outcome.Moved || outcome.Blocking.Count == 0 || notice == null)
                return;

            await notice.ShowAsync(dropService.DescribeBlock(source, target, outcome.Blocking));
        }

        // The node that takes dropped files: the first composition or layer from the node up. No node, or a node
        // that is under no composition, gives null, and then the drop makes a new composition.
        public TreeNode? FindPathTarget(TreeNode? node)
        {
            for (var current = node; current != null; current = current.Parent)
                if (current.Item is Composition or Layer)
                    return current;

            return null;
        }

        // True when the node itself takes the dropped files: a composition, a layer, or the group of entities of a layer.
        // The other nodes of a layer pass the drop to the entities of their layer, but they are not the target themselves.
        public bool IsPathTarget(TreeNode node) =>
            node.Item is Composition or Layer
            || node.Group is EntitiesGroup;

        // True when DropPaths makes an item from this path.
        public bool AcceptsPath(string path) => pathDrop.AcceptsItem(path);

        public bool CanDropPaths(IEnumerable<string> paths) => paths.Any(pathDrop.AcceptsItem);

        // Makes what the dropped paths need, and adds an item for each path, as one undo step.
        // A composition takes a new layer, and a layer takes the items. With no target, a new composition with a new
        // layer takes them. A drop of nothing that is accepted changes nothing.
        public void DropPaths(TreeNode? hovered, IReadOnlyList<string> paths)
        {
            var accepted = paths.Where(pathDrop.AcceptsItem).ToList();
            if (accepted.Count == 0)
                return;

            var target = FindPathTarget(hovered);
            undoManager.BeginCapture();
            try
            {
                switch (target?.Item)
                {
                    case Layer layer:
                        AddPaths(layer, accepted);
                        break;
                    case Composition composition:
                        AddLayerWithPaths(composition, accepted);
                        break;
                    default:
                        compositions.AddItem(typeof(Composition), item => AddLayerWithPaths((Composition)item, accepted));
                        break;
                }
            }
            finally
            {
                undoManager.EndCapture();
            }
        }

        private void AddLayerWithPaths(Composition composition, IReadOnlyList<string> paths) =>
            composition.LayerManager.AddItem(typeof(Layer), item => AddPaths((Layer)item, paths));

        private void AddPaths(Layer layer, IEnumerable<string> paths)
        {
            foreach (var path in paths)
                pathDrop.AddItem(layer.ModelEntityManager, path);
        }

        // The binding of the tree view sets this. The origin is Pointer.
        public TreeNode? SelectedNode
        {
            get => selectedNode;
            set => Select(value, SelectionOrigin.Pointer);
        }

        // What made the last selection. The view brings a row into view only for some origins.
        public SelectionOrigin Origin { get; private set; } = SelectionOrigin.Program;

        // Selects a node and its chain in the lists. The origin is set before the change event.
        public void Select(TreeNode? node, SelectionOrigin origin)
        {
            Origin = origin;
            if (!SetProperty(ref selectedNode, node, nameof(SelectedNode)) || node == null || selecting)
                return;

            SelectChain(node);
        }

        // The node of an item, wherever it is in the tree. A moved item has a new node in its new list.
        private static TreeNode? FindNode(TreeNode from, IControl item)
        {
            foreach (var child in from.Children)
            {
                if (ReferenceEquals(child.Item, item))
                    return child;

                if (FindNode(child, item) is { } found)
                    return found;
            }

            return null;
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
            if (selecting || ReferenceEquals(selectedNode, node))
                return;

            // A local list and a peer both come here. The tree cannot tell them apart, so the origin is Program.
            Origin = SelectionOrigin.Program;
            SetProperty(ref selectedNode, node, nameof(SelectedNode));
        }

        // The root and the Compositions row, which is the one row under it, are the top of the tree. A selection does not move there.
        private bool IsTop(TreeNode? node) => node == null || ReferenceEquals(node, root) || ReferenceEquals(node.Parent, root);

        // A node leaves the tree. A selection inside it moves to the parent.
        internal void NodeRemoved(TreeNode node)
        {
            if (softSelectedNode != null && softSelectedNode.IsOrIsUnder(node))
                SoftSelectedNode = selectedNode != null && !selectedNode.IsOrIsUnder(node) ? selectedNode : null;

            if (selectedNode == null || !selectedNode.IsOrIsUnder(node))
                return;

            Origin = SelectionOrigin.Program;
            SetProperty(ref selectedNode, IsTop(node.Parent) ? null : node.Parent, nameof(SelectedNode));
        }

        public void Dispose() => root.Dispose();
    }

    // What made a selection of the tree.
    public enum SelectionOrigin
    {
        Program,
        Pointer,
        Keyboard,
        Peer
    }
}
