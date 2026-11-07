// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Controls.Collection;
using CMiX.UI.Controls.Parts;
using CMiX.UI.Popups;

namespace CMiX.Studio.Avalonia.Views
{
    // The panel that adds something under a node of the tree: a card with a search field and the groups of what the node can take
    // (see ProjectTree.GetAddGroups). The row menu, the + of a row, the toolbar and the shortcut all open this one panel.
    // The panel does not know where it was opened from. The caller gives an anchor and a side. The panel puts itself on that side,
    // and its tip points at the anchor.
    // On the right or the left of the anchor, the search field lines up with the middle of the anchor, so the pointer moves only
    // a little to reach the search field and the first rows. Above or below the anchor, the tip is on the middle of the anchor.
    // A pick closes the panel and then runs the add, so the lists change after the popup is gone.
    public sealed class AddPanel
    {
        // Under the anchor, the card starts this far before the anchor.
        private const double BelowOverhang = 8;

        // The space between the header and the list inside the card (see the StackPanel).
        private const double Spacing = 4;

        private readonly Flyout flyout = new() { ShowMode = FlyoutShowMode.Standard };
        private readonly PopupAnchoring anchoring;
        private readonly PopupCard card = new();
        private readonly TextBlock title = new() { VerticalAlignment = VerticalAlignment.Center };
        private readonly GroupedSearchList list = new();
        private readonly Dictionary<TreeCategory, IBrush?> brushes = new();

        public AddPanel()
        {
            var content = new StackPanel { Spacing = Spacing };
            content.Children.Add(title);
            content.Children.Add(list);
            card.Content = content;
            flyout.Content = card;

            anchoring = PopupAnchoring.Use(flyout);
            anchoring.Placed += result => card.Place(result);

            list.Picked += OnPicked;
            list.Cancelled += (_, _) => Close();
        }

        public bool IsOpen => flyout.IsOpen;

        // Raised when the panel has closed, after a pick, a click away or Escape.
        public event EventHandler? Closed
        {
            add => flyout.Closed += value;
            remove => flyout.Closed -= value;
        }

        // For the tests: the rule of the place, the list and the card.
        internal PopupAnchoring Anchoring => anchoring;
        internal GroupedSearchList List => list;
        internal PopupCard Card => card;
        internal TextBlock Title => title;

        // Opens the panel on one side of the anchor. The header is a line of text above the search field, such as "Add to Layer 02".
        // Nothing opens when there is nothing to add.
        public void Open(Control anchor, PopupSide side, IReadOnlyList<TreeAddGroup> groups, string? header = null)
        {
            if (groups.Count == 0)
                return;

            Close();

            var rowHeight = Number(anchor, "ControlHeightMD", 24);
            title.Text = header;
            title.IsVisible = !string.IsNullOrEmpty(header);
            title.Height = rowHeight;
            list.Width = Number(anchor, "AddPanelWidth", 200);
            list.KindBrush = kind => BrushOf(anchor, kind);
            list.Groups = groups.Select(ToPicker).ToList();

            anchoring.Side = side;
            anchoring.Along = null;
            anchoring.AlignOffset = side is PopupSide.Left or PopupSide.Right
                ? SearchCenter(anchor, rowHeight)
                : anchor.Bounds.Width / 2 + BelowOverhang;

            card.ShowsTip = false;
            if (anchor.TryFindResource("FlyoutPresenterCard", out var theme) && theme is ControlTheme presenterTheme)
                flyout.FlyoutPresenterTheme = presenterTheme;

            flyout.ShowAt(anchor);
        }

        public void Close() => flyout.Hide();

        // The distance from the top of the card to the middle of the search field: the border, the padding, the header and its space.
        private double SearchCenter(Control scope, double rowHeight)
        {
            var padding = scope.TryFindResource("PopupCardPadding", out var value) && value is Thickness thickness ? thickness : new Thickness(10, 8);
            var header = title.IsVisible ? rowHeight + Spacing : 0;
            return 1 + padding.Top + header + rowHeight / 2;
        }

        private void OnPicked(object? sender, PickerPickedEventArgs e)
        {
            var add = e.Item.Value as TreeAdd;
            Close();

            // Posted so the popup is gone before the lists change.
            Dispatcher.UIThread.Post(() => add?.Execute?.Invoke());
        }

        private static PickerGroup ToPicker(TreeAddGroup group) =>
            new(group.Header, group.Items.Select(item => new PickerItem(item.Header, item, item.Category)).ToList(), group.Category);

        // The bright color of a kind, the same one as the tree row of that kind.
        private IBrush? BrushOf(Control scope, object? kind)
        {
            if (kind is not TreeCategory category)
                return null;

            if (!brushes.TryGetValue(category, out var brush))
            {
                brush = scope.TryFindResource("Category" + category, out var color) && color is Color value ? new SolidColorBrush(value) : null;
                brushes[category] = brush;
            }

            return brush;
        }

        private static double Number(Control scope, string key, double fallback) =>
            scope.TryFindResource(key, out var value) && value is double number ? number : fallback;
    }
}
