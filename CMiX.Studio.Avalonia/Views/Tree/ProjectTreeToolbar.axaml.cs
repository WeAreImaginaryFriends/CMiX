// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views.Managers;
using CMiX.UI.Controls.Parts;

namespace CMiX.Studio.Avalonia.Views
{
    // The toolbar under the tree of the project. The buttons are commands of the tree.
    // The Add button adds at once when the selected node has one thing to add. With several, it opens a menu of them.
    public partial class ProjectTreeToolbar : UserControl
    {
        // The space between the panel of the menu of the Add button and the button.
        private const double MenuGap = 8;

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

            // The menu stands on the button: the bottom left corner of its panel is just over the top left corner of the button.
            // The popup is larger than its panel by the margin of the shadow, so the offsets take that margin out.
            menu.Placement = PlacementMode.AnchorAndGravity;
            menu.PlacementAnchor = PopupAnchor.TopLeft;
            menu.PlacementGravity = PopupGravity.TopRight;
            menu.HorizontalOffset = -PopupFrame.ShadowMargin.Left;
            menu.VerticalOffset = PopupFrame.ShadowMargin.Bottom - MenuGap;

            menu.Closed += (_, _) => AddButton.ContextMenu = null;
            AddButton.ContextMenu = menu;
            menu.Open(AddButton);
        }
    }
}
