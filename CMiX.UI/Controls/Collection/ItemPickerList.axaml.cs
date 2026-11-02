// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;

namespace CMiX.UI.Controls.Collection
{
    // A list of rows to pick from. It shows at most ComboBoxVisibleItems rows and scrolls beyond them.
    public partial class ItemPickerList : ItemPickerBase
    {
        public ItemPickerList()
        {
            InitializeComponent();
            PART_Items.ItemTemplate = new FuncDataTemplate<object>((item, _) => Row(item));
        }

        private Button Row(object item)
        {
            var row = new Button { CommandParameter = item };

            if (this.TryFindResource("ButtonItem", ActualThemeVariant, out var theme) && theme is ControlTheme controlTheme)
                row.Theme = controlTheme;

            row.Bind(Button.CommandProperty, new Binding(nameof(ItemCommand)) { Source = this });

            if (string.IsNullOrEmpty(TextPath))
                row.Content = item.ToString();
            else
                row.Bind(ContentControl.ContentProperty, new Binding(TextPath) { Source = item });

            return row;
        }
    }
}
