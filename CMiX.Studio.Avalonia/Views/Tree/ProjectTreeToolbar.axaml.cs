// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;
using Avalonia.Interactivity;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Popups;

namespace CMiX.Studio.Avalonia.Views
{
    // The toolbar over the tree of the project. The buttons are commands of the tree.
    // The Add button opens the Add panel under the button. With nothing selected, the panel offers a composition.
    public partial class ProjectTreeToolbar : UserControl
    {
        private readonly AddPanel panel = new();

        public ProjectTreeToolbar()
        {
            InitializeComponent();
        }

        // For the tests: the panel of the Add button.
        internal AddPanel Panel => panel;

        // The panel shows what the selected node takes (ProjectTree.GetAddGroups), then the Project group, as a composition can be added
        // whatever is selected. Its header names the node, because the button is not on a row.
        private void OnAddClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return;

            // The button always opens the panel, also with nothing selected, when the panel has only the composition to offer:
            // a click on the button must not add a composition by surprise.
            if (tree.SelectedAdds.Count == 0)
                return;

            var node = tree.SelectedNode;
            panel.Open(AddButton, PopupSide.Bottom, tree.GetAddGroups(node), node == null ? null : "Add to " + node.Header);
        }
    }
}
