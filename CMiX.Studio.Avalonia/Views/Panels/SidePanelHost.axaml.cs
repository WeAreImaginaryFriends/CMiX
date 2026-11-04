// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CMiX.Studio.Avalonia.ViewModels;

namespace CMiX.Studio.Avalonia.Views
{
    // A panel of the window with a combo box at the top, in which the user picks what the panel shows.
    public partial class SidePanelHost : UserControl
    {
        public static readonly StyledProperty<PanelChoice?> ChoiceProperty =
            AvaloniaProperty.Register<SidePanelHost, PanelChoice?>(nameof(Choice), defaultBindingMode: BindingMode.TwoWay);

        public PanelChoice? Choice
        {
            get => GetValue(ChoiceProperty);
            set => SetValue(ChoiceProperty, value);
        }

        public static readonly StyledProperty<IEnumerable?> ChoicesProperty =
            AvaloniaProperty.Register<SidePanelHost, IEnumerable?>(nameof(Choices));

        public IEnumerable? Choices
        {
            get => GetValue(ChoicesProperty);
            set => SetValue(ChoicesProperty, value);
        }

        public SidePanelHost()
        {
            InitializeComponent();
        }
    }
}
