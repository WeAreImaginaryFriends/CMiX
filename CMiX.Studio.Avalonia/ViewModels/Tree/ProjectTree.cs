// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CMiX.Studio.Avalonia.ViewModels.Tree
{
    // The compositions of the project with their layers, and the entities, cameras and lights of each layer.
    // The tree follows the lists, also when a peer changes them.
    // A selected node selects its whole chain in the lists, as one undo step.
    public sealed class ProjectTree : ObservableObject, IDisposable
    {
        private readonly UndoManager undoManager;
        private readonly TreeNode root;
        private TreeNode? selectedNode;
        private bool selecting;

        public ProjectTree(Project project, UndoManager undoManager)
        {
            this.undoManager = undoManager;
            root = new TreeNode(this, null, null, null, "Project", project.CompositionManager);
        }

        public ObservableCollection<TreeNode> Roots => root.Children;

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
