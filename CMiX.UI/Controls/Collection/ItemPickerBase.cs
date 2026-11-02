// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace CMiX.UI.Controls.Collection
{
    // The input of the lists that offer items to pick. A click on an item runs ItemCommand with the item.
    // TextPath is the path of the text of an item, such as PrefabService.Name.Value. Empty gives the ToString of the item.
    public class ItemPickerBase : UserControl
    {
        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<ItemPickerBase, IEnumerable?>(nameof(ItemsSource));

        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly StyledProperty<ICommand?> ItemCommandProperty =
            AvaloniaProperty.Register<ItemPickerBase, ICommand?>(nameof(ItemCommand));

        public ICommand? ItemCommand
        {
            get => GetValue(ItemCommandProperty);
            set => SetValue(ItemCommandProperty, value);
        }

        public static readonly StyledProperty<string> TextPathProperty =
            AvaloniaProperty.Register<ItemPickerBase, string>(nameof(TextPath), string.Empty);

        public string TextPath
        {
            get => GetValue(TextPathProperty);
            set => SetValue(TextPathProperty, value);
        }
    }
}
