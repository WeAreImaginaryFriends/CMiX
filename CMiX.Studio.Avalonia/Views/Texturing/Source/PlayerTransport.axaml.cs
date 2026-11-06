// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using CMiX.Studio.Avalonia.Views.Modulation;

namespace CMiX.Studio.Avalonia.Views
{
    // The Play, Loop and Do Seek rows of a player. The DataContext is the player.
    public partial class PlayerTransport : UserControlModulatable
    {
        public static readonly StyledProperty<bool> HasLoopProperty =
            AvaloniaProperty.Register<PlayerTransport, bool>(nameof(HasLoop));

        public bool HasLoop
        {
            get => GetValue(HasLoopProperty);
            set => SetValue(HasLoopProperty, value);
        }

        public PlayerTransport()
        {
            InitializeComponent();
            UpdateLoopRow();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == HasLoopProperty)
                UpdateLoopRow();
        }

        // A hidden row still gets a position, so the Loop row must leave the panel.
        // Then the panel gives Top and Bottom to the rows that stay.
        private void UpdateLoopRow()
        {
            if (transportPanel is null || loopRow is null)
                return;

            bool inPanel = transportPanel.Children.Contains(loopRow);
            if (HasLoop && !inPanel)
                transportPanel.Children.Insert(1, loopRow);
            else if (!HasLoop && inPanel)
                transportPanel.Children.Remove(loopRow);
        }
    }
}
