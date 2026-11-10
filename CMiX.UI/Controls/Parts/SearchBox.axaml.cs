// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace CMiX.UI.Controls.Parts
{
    // The search field with a magnifier. The lists with a search (GroupedSearchList, SearchableItemPickerList) and the drop-downs with a
    // search (see ComboBoxSearch) use this one control, so every search field looks and works the same. A key of the field
    // goes up to this control as a normal routed event.
    public partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            InitializeComponent();
        }

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<SearchBox, string>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<SearchBox, string>(nameof(Watermark), "Search");

        public string Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        // True: the field keeps its text and takes no typing.
        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<SearchBox, bool>(nameof(IsReadOnly));

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        // Gives the keyboard focus to the field itself.
        public void FocusInput() => PART_Search.Focus();

        // Selects the whole text of the field, so the next typing replaces it.
        public void SelectAll() => PART_Search.SelectAll();
    }
}
