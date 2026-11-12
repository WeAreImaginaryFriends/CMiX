// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Popups;

namespace CMiX.Studio.Avalonia.Views
{
    // The toolbar over the tree of the project. The buttons are commands of the tree.
    // A click on the Add button opens the Add panel with its search field under the pointer, as the shortcut Tree.Add does. A button
    // that the keyboard clicks opens the panel under the button. With nothing selected, the panel offers a composition.
    public partial class ProjectTreeToolbar : UserControl
    {
        private readonly AddPanel panel = new();

        // The last place of the pointer over the toolbar, in the coordinates of the toolbar.
        private Point lastPointer;

        public ProjectTreeToolbar()
        {
            InitializeComponent();
            AddHandler(PointerMovedEvent, (_, e) => lastPointer = e.GetPosition(this), RoutingStrategies.Tunnel, handledEventsToo: true);
            AddHandler(PointerPressedEvent, (_, e) => lastPointer = e.GetPosition(this), RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        // For the tests: the panel of the Add button.
        internal AddPanel Panel => panel;

        // The panel shows what the target node takes (ProjectTree.TargetNode, GetAddGroups), then the Project group, as a composition can be
        // added whatever the target is. Its header names the node, because the button is not on a row.
        private void OnAddClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return;

            // The button always opens the panel, also with no target, when the panel has only the composition to offer:
            // a click on the button must not add a composition by surprise.
            if (tree.TargetAdds.Count == 0)
                return;

            var node = tree.TargetNode;
            var header = node == null ? null : "Add to " + node.Header;
            if (AddButton.IsPointerOver)
                panel.OpenAtPoint(this, lastPointer, tree.GetAddGroups(node), header);
            else
                panel.Open(AddButton, PopupSide.Bottom, tree.GetAddGroups(node), header);
        }
    }
}
