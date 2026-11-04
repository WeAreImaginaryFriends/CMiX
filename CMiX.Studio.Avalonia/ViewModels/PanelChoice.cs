// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;

namespace CMiX.Studio.Avalonia.ViewModels
{
    // What a side panel of the main window can show. The user picks it in the combo box at the top of the panel.
    // Each choice is its own type, so the panel picks the view with a data template.
    // To add a choice: add a type here, add it to All, and add a data template for it in SidePanelHost.
    public abstract class PanelChoice
    {
        protected PanelChoice(string name)
        {
            Name = name;
        }

        // The text in the combo box.
        public string Name { get; }

        public static PanelChoice Tree { get; } = new TreePanelChoice();
        public static PanelChoice Tabs { get; } = new TabsPanelChoice();

        public static IReadOnlyList<PanelChoice> All { get; } = new[] { Tree, Tabs };

        public override string ToString() => Name;
    }

    // The project tree.
    public sealed class TreePanelChoice : PanelChoice
    {
        public TreePanelChoice() : base("Tree") { }
    }

    // The tabs of the app, with the master beat under them.
    public sealed class TabsPanelChoice : PanelChoice
    {
        public TabsPanelChoice() : base("Tabs") { }
    }
}
