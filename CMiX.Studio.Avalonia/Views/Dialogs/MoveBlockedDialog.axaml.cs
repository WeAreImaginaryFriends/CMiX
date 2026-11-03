// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CMiX.Studio.Avalonia.Views.Dialogs
{
    // Tells the user why a move in the project tree is not possible. OK or Escape closes it.
    public partial class MoveBlockedDialog : Window
    {
        public MoveBlockedDialog() : this(string.Empty)
        {
        }

        public MoveBlockedDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void OnOk(object? sender, RoutedEventArgs e) => Close();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key != Key.Escape && e.Key != Key.Enter)
                return;

            Close();
            e.Handled = true;
        }
    }
}
