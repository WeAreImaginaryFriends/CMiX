// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.ComponentModel;
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
    // Escape clears the text, and Escape in an empty field gives the focus back to the rows. Enter moves the cursor of the tree to the
    // next match, and Shift+Enter to the match before. The selection does not change, and the field keeps the focus. Down gives the focus
    // to the cursor row. During a drag, Escape ends the drag without a drop, as it does in the tree, and the other keys do nothing.
    // The clear button of the field empties the text, as Escape does, and the field keeps the focus. The text stays when the focus goes
    // out of the field: the rows stay dimmed until the text is cleared, by Escape in the tree or by Tree.ClearSearch (Alt+F).
    public partial class TreeSearchBox : UserControl
    {
        private ProjectTree? watched;

        // The class of the field while a search filters the rows and the focus is not in the field (see TreeSearchBox.axaml).
        internal const string FilteringClass = "filtering";

        public TreeSearchBox()
        {
            InitializeComponent();
            searchField.AddHandler(KeyDownEvent, OnFieldKeyDown, RoutingStrategies.Tunnel);
            searchField.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsKeyboardFocusWithinProperty)
                    UpdateFiltering();
            };
        }

        private void UpdateFiltering() =>
            searchField.Classes.Set(FilteringClass, watched?.IsSearchActive == true && !searchField.IsKeyboardFocusWithin);

        private void OnWatchedPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectTree.IsSearchActive))
                UpdateFiltering();
        }

        // For the tests: the field, and its clear button.
        internal SearchBox Field => searchField;
        internal Button Clear => searchField.GetVisualDescendants().OfType<Button>().First(button => button.Name == SearchBox.ClearPartName);

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (watched != null)
            {
                watched.SearchFocusRequested -= OnSearchFocusRequested;
                watched.PropertyChanged -= OnWatchedPropertyChanged;
            }

            watched = DataContext as ProjectTree;
            if (watched != null)
            {
                watched.SearchFocusRequested += OnSearchFocusRequested;
                watched.PropertyChanged += OnWatchedPropertyChanged;
            }

            UpdateFiltering();
        }

        // The focus waits for the layout, in case the panel shows the tree only now. With two panels that show the tree, only the box of
        // the panel of the active view answers (see ProjectTreeView.IsActiveView). The focus in this box makes its panel the active
        // panel, also with the pointer over the other tree. A box with no tree view in its panel answers.
        private void OnSearchFocusRequested(object? sender, EventArgs e)
        {
            if (TreeViewOfPanel() is { IsActiveView: false })
                return;

            Dispatcher.UIThread.Post(() =>
            {
                searchField.FocusInput();
                searchField.SelectAll();
            }, DispatcherPriority.Loaded);
        }

        // The tree view in the same side panel as this box.
        private ProjectTreeView? TreeViewOfPanel() =>
            this.FindAncestorOfType<SidePanelHost>()?.GetVisualDescendants().OfType<ProjectTreeView>().FirstOrDefault();

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
                case Key.Enter when e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift:
                    tree.MoveCursorToMatch(e.KeyModifiers == KeyModifiers.Shift ? -1 : 1);
                    break;
                case Key.Down when e.KeyModifiers == KeyModifiers.None:
                    tree.FocusTree();
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }
    }
}
