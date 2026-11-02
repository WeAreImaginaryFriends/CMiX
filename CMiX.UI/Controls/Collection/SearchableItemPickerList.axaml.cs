// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;

namespace CMiX.UI.Controls.Collection
{
    // An ItemPickerList with a search field above it. The field takes the focus when the list appears, and the
    // rows are the items whose text holds the search text, in any case. The first row is highlighted. Down and Up move the
    // highlight, Enter picks the highlighted row, and Escape clears the text.
    // The list shows the text of the items as they were when it was filtered. A name that changes while the list is open
    // shows in the filter at the next key.
    public partial class SearchableItemPickerList : ItemPickerBase
    {
        private INotifyCollectionChanged? _watched;

        static SearchableItemPickerList()
        {
            ItemsSourceProperty.Changed.AddClassHandler<SearchableItemPickerList>((list, e) => list.WatchItems(e.NewValue as INotifyCollectionChanged));
            SearchTextProperty.Changed.AddClassHandler<SearchableItemPickerList>((list, _) => list.Filter());
            TextPathProperty.Changed.AddClassHandler<SearchableItemPickerList>((list, _) => list.Filter());
        }

        public SearchableItemPickerList()
        {
            InitializeComponent();
            PART_Search.KeyDown += OnSearchKeyDown;
        }

        public static readonly StyledProperty<string> SearchTextProperty =
            AvaloniaProperty.Register<SearchableItemPickerList, string>(nameof(SearchText), string.Empty);

        public string SearchText
        {
            get => GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        // The items that match the search text, in the order of the source.
        public ObservableCollection<object> Matches { get; } = new();

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // Each time the list appears it starts without a filter, with the cursor in the field.
            SearchText = string.Empty;
            Filter();
            Dispatcher.UIThread.Post(() => PART_Search.Focus(), DispatcherPriority.Input);
        }

        private void WatchItems(INotifyCollectionChanged? items)
        {
            if (_watched != null)
                _watched.CollectionChanged -= OnItemsChanged;

            _watched = items;

            if (_watched != null)
                _watched.CollectionChanged += OnItemsChanged;

            Filter();
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Filter();

        private void Filter()
        {
            Matches.Clear();

            var any = false;
            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                {
                    any = true;
                    if (IsMatch(item))
                        Matches.Add(item);
                }
            }

            // The field and the line exist after the XAML is loaded.
            if (PART_NoMatch != null)
                PART_NoMatch.IsVisible = any && Matches.Count == 0;

            // A new filter starts on the first row.
            Highlight(0);
        }

        private bool IsMatch(object item) =>
            TextOf(item).Contains(SearchText ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);

        // The text at the path of the item, such as PrefabService.Name.Value.
        private string TextOf(object item)
        {
            if (string.IsNullOrEmpty(TextPath))
                return item.ToString() ?? string.Empty;

            object? value = item;
            foreach (var part in TextPath.Split('.'))
            {
                if (value == null)
                    break;
                value = value.GetType().GetProperty(part)?.GetValue(value);
            }
            return value?.ToString() ?? string.Empty;
        }

        // The index of the row that Enter picks, in the rows that match. It is -1 when no row matches.
        public int HighlightedIndex { get; private set; } = -1;

        // The index is kept inside the rows. The list scrolls to the row.
        private void Highlight(int index)
        {
            HighlightedIndex = Matches.Count == 0 ? -1 : Math.Clamp(index, 0, Matches.Count - 1);

            if (PART_List != null)
                PART_List.HighlightedItem = HighlightedIndex < 0 ? null : Matches[HighlightedIndex];
        }

        private void OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    Highlight(HighlightedIndex + 1);
                    e.Handled = true;
                    break;

                case Key.Up:
                    Highlight(HighlightedIndex - 1);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    var picked = HighlightedIndex < 0 ? null : Matches[HighlightedIndex];
                    if (picked != null && ItemCommand?.CanExecute(picked) == true)
                    {
                        ItemCommand.Execute(picked);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape when SearchText.Length > 0:
                    SearchText = string.Empty;
                    e.Handled = true;
                    break;
            }
        }
    }
}
