// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using Avalonia;
using Avalonia.Controls;
using CMiX.Core.Compositing;

namespace CMiX.Studio.Avalonia.Views
{
    // The visibility toggle and the mask toggle of an item. The DataContext is the item.
    public partial class ComposableToggles : UserControl
    {
        public static readonly DirectProperty<ComposableToggles, IComposable?> ComposableProperty =
            AvaloniaProperty.RegisterDirect<ComposableToggles, IComposable?>(nameof(Composable), toggles => toggles.Composable);

        private IComposable? composable;

        public ComposableToggles()
        {
            InitializeComponent();
        }

        // The item that has a mask, or null.
        public IComposable? Composable
        {
            get => composable;
            private set => SetAndRaise(ComposableProperty, ref composable, value);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            Composable = DataContext as IComposable;
        }
    }
}
