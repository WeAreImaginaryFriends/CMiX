// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CommunityToolkit.Mvvm.Input;

namespace CMiX.Studio.Avalonia.Services
{
    // Moves the keyboard focus between the panels of the main window (Window.NextPanel, Window.PreviousPanel, Window.FocusPanel1 to 4)
    // and to the tree (Tree.Focus). The panels are the visible side panels (SidePanelHost), from left to right. Each panel keeps its
    // last focused control. The focus goes back to that control, else to the first control of the panel that can take the focus.
    public sealed class PanelFocusCycle
    {
        private readonly ProjectTree tree;
        private readonly Dictionary<SidePanelHost, Control> lastFocused = new();
        private TopLevel? window;

        public PanelFocusCycle(ProjectTree tree)
        {
            this.tree = tree;
            NextPanelCommand = new RelayCommand(() => MoveBy(1));
            PreviousPanelCommand = new RelayCommand(() => MoveBy(-1));
            FocusPanel1Command = new RelayCommand(() => FocusPanelAt(0));
            FocusPanel2Command = new RelayCommand(() => FocusPanelAt(1));
            FocusPanel3Command = new RelayCommand(() => FocusPanelAt(2));
            FocusPanel4Command = new RelayCommand(() => FocusPanelAt(3));
            FocusTreeCommand = new RelayCommand(tree.FocusTree);
        }

        public IRelayCommand NextPanelCommand { get; }
        public IRelayCommand PreviousPanelCommand { get; }

        // A panel that is not there does nothing.
        public IRelayCommand FocusPanel1Command { get; }
        public IRelayCommand FocusPanel2Command { get; }
        public IRelayCommand FocusPanel3Command { get; }
        public IRelayCommand FocusPanel4Command { get; }

        // The focus goes to the cursor row of the tree, in the panel that shows the tree (see ProjectTree.FocusTree).
        public IRelayCommand FocusTreeCommand { get; }

        // The main window calls this once. The cycle then records the focused control of each panel.
        public void Attach(TopLevel mainWindow)
        {
            window = mainWindow;
            mainWindow.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        // The visible side panels, from left to right.
        public IReadOnlyList<SidePanelHost> Panels()
        {
            if (window is not { } top)
                return new List<SidePanelHost>();

            return top.GetVisualDescendants().OfType<SidePanelHost>()
                .Where(panel => panel.IsEffectivelyVisible)
                .OrderBy(panel => panel.TranslatePoint(default, top)?.X ?? 0)
                .ToList();
        }

        private void OnGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (e.Source is Control control && PanelOf(control) is { } panel)
                lastFocused[panel] = control;
        }

        private static SidePanelHost? PanelOf(Visual visual) =>
            visual as SidePanelHost ?? visual.FindAncestorOfType<SidePanelHost>();

        private SidePanelHost? FocusedPanel() =>
            window?.FocusManager?.GetFocusedElement() is Visual focused ? PanelOf(focused) : null;

        // With no panel focused, the next panel is the first panel and the previous panel is the last panel.
        private void MoveBy(int step)
        {
            var panels = Panels();
            if (panels.Count == 0)
                return;

            var current = FocusedPanel() is { } focused ? IndexOf(panels, focused) : -1;
            var next = current < 0
                ? (step > 0 ? 0 : panels.Count - 1)
                : (current + step + panels.Count) % panels.Count;
            FocusPanel(panels[next]);
        }

        private static int IndexOf(IReadOnlyList<SidePanelHost> panels, SidePanelHost panel)
        {
            for (var i = 0; i < panels.Count; i++)
                if (ReferenceEquals(panels[i], panel))
                    return i;

            return -1;
        }

        private void FocusPanelAt(int index)
        {
            var panels = Panels();
            if (index < panels.Count)
                FocusPanel(panels[index]);
        }

        public void FocusPanel(SidePanelHost panel)
        {
            if (lastFocused.TryGetValue(panel, out var last) && CanFocus(last) && panel.IsVisualAncestorOf(last) && last.Focus(NavigationMethod.Tab))
                return;

            FocusFirst(panel);
        }

        // The first control of the content of the panel, else of its bar. In a panel of the tree, the focus then goes to the cursor row.
        private void FocusFirst(SidePanelHost panel)
        {
            Visual content = panel.FindControl<ContentControl>("content") is { } found ? found : panel;
            var first = FirstFocusable(content) ?? FirstFocusable(panel);
            if (first == null || !first.Focus(NavigationMethod.Tab))
                return;

            if (panel.GetVisualDescendants().OfType<ProjectTreeView>().Any())
                tree.FocusTree();
        }

        private static InputElement? FirstFocusable(Visual root) =>
            root.GetVisualDescendants().OfType<InputElement>().FirstOrDefault(CanFocus);

        private static bool CanFocus(InputElement element) =>
            element.Focusable && element.IsEffectivelyEnabled && element.IsEffectivelyVisible && element.GetVisualRoot() != null;
    }
}
