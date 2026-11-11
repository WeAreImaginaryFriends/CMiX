// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.UI.Controls.Parts;

namespace CMiX.Studio.Avalonia.Views
{
    // The search field of the project tree (see TreeSearchBox.axaml). Tree.Search gives it the focus (ProjectTree.FocusSearch).
    // Escape clears the text, and Escape in an empty field gives the focus back to the rows. Enter and F3 select the next match, and
    // Shift with them the match before. During a drag, Escape ends the drag without a drop, as it does in the tree, and the other keys
    // do nothing. The clear button of the field empties the text, as Escape does, and the field keeps the focus.
    public partial class TreeSearchBox : UserControl
    {
        private ProjectTree? watched;

        public TreeSearchBox()
        {
            InitializeComponent();
            searchField.AddHandler(KeyDownEvent, OnFieldKeyDown, RoutingStrategies.Tunnel);
        }

        // For the tests: the field, and its clear button.
        internal SearchBox Field => searchField;
        internal Button Clear => searchField.GetVisualDescendants().OfType<Button>().First(button => button.Name == SearchBox.ClearPartName);

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (watched != null)
                watched.SearchFocusRequested -= OnSearchFocusRequested;

            watched = DataContext as ProjectTree;
            if (watched != null)
                watched.SearchFocusRequested += OnSearchFocusRequested;
        }

        // The focus waits for the layout, in case the panel shows the tree only now.
        private void OnSearchFocusRequested(object? sender, EventArgs e) =>
            Dispatcher.UIThread.Post(() =>
            {
                searchField.FocusInput();
                searchField.SelectAll();
            }, DispatcherPriority.Loaded);

        private void OnFieldKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not ProjectTree tree)
                return;

            if (tree.IsDragging)
            {
                if (e.Key == Key.Escape)
                {
                    tree.CancelDrag();
                    e.Handled = true;
                }

                return;
            }

            switch (e.Key)
            {
                case Key.Escape when tree.SearchText.Length > 0:
                    tree.SearchText = string.Empty;
                    break;
                case Key.Escape:
                    tree.FocusTree();
                    break;
                case Key.Enter or Key.F3:
                    tree.SelectMatch(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }
    }
}
