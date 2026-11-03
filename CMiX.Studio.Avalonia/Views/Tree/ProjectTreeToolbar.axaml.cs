// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;
using Avalonia.Interactivity;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;

namespace CMiX.Studio.Avalonia.Views
{
    // The toolbar under the tree of the project. The buttons are commands of the tree.
    // The Add button adds at once when the selected node has one thing to add. With several, it opens a menu of them.
    public partial class ProjectTreeToolbar : UserControl
    {
        public ProjectTreeToolbar()
        {
            InitializeComponent();
        }

        // Runs before the command of the button. The command adds only when there is one choice.
        private void OnAddClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return;

            var choices = tree.GetAddChoices(tree.SelectedNode);
            if (choices.Count < 2)
                return;

            AddButton.ContextMenu?.Close();
            var menu = PrefabRowMenu.Create(AddButton, RowMenuKind.Group, () => null, choices.ToRows(), () => tree.SelectedNode?.Header ?? string.Empty);
            menu.Closed += (_, _) => AddButton.ContextMenu = null;
            AddButton.ContextMenu = menu;
            menu.Open(AddButton);
        }
    }
}
