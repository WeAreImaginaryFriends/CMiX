// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia.Controls;

namespace CMiX.Studio.Avalonia.Views
{
    // The squares with the toggles of a row of the tree: the visibility toggle first, then the mask toggle.
    public partial class RowToggles : UserControl
    {
        // The number of squares in the column. A new toggle adds one.
        public const int Slots = 2;

        public RowToggles()
        {
            InitializeComponent();
        }

        // The width of the column for a row height and a space: the space before the first square, then the squares.
        public static double WidthFor(double rowHeight, double spacing) => Slots * (rowHeight + spacing);
    }
}
