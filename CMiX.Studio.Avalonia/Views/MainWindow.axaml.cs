// SPDX-FileCopyrightText: 2017-2026 CloneProduction Shanghai Company Limited and CMiX contributors
// SPDX-License-Identifier: LGPL-3.0-or-later

using Avalonia;
using Avalonia.Controls;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using CMiX.UI.Behaviors;
using Avalonia.LogicalTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.ViewModels;

namespace CMiX.Studio.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private readonly UndoManager? _undoManager;
        public MainWindow(UndoManager undoManager) : this()
        {
            _undoManager = undoManager;
        }

        public MainWindow()
        {
            InitializeComponent();
            titleBar.PointerPressed += TitleBar_PointerPressed;
            Closing += OnClosing;
            Opened += (_, _) => { if (DockOnOpen) WindowDock.DockRight(this); };
            InstallToggleDrag();
            // A text box gets Ctrl+Z first. A key that it does not handle still undoes or redoes the project.
            KeyBindingTextGuard.Install(this, afterTextBox: true);
            AddHandler(ProjectTreeView.NodeChosenEvent, (_, e) => ShowInspector(e.Source as Visual));
        }

        // The window opens on the right edge of the screen, as high as the working area. A program can turn this off.
        public bool DockOnOpen { get; set; } = true;

        // A click on a row of the tree shows the inspector, which has the editor of that row. A program can turn this off.
        public bool OpenInspectorOnTreeClick { get; set; } = true;

        // Shows the inspector in the tabs beside the tree. The side panel that holds the tree keeps its content.
        private void ShowInspector(Visual? tree)
        {
            if (!OpenInspectorOnTreeClick)
                return;

            var side = tree?.FindAncestorOfType<SidePanelHost>();
            foreach (var tabs in this.GetVisualDescendants().OfType<MainTabsPanel>().ToList())
                if (side == null || !side.IsVisualAncestorOf(tabs))
                    tabs.ShowInspector();
        }

        // A drag over the visibility or the mask buttons sets them all to one state. The whole drag is one undo step.
        private void InstallToggleDrag()
        {
            var toggleDrag = new ToggleDragBehavior();
            toggleDrag.StrokeStarted += (_, _) => _undoManager?.BeginCapture();
            toggleDrag.StrokeEnded += (_, _) => _undoManager?.EndCapture();
            Interaction.GetBehaviors(this).Add(toggleDrag);
        }

        // The cross button, Alt+F4 and the close command all end here. With unsaved changes the close is
        // put off until the user has answered. The window then closes for good, without asking again.
        private async void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (DataContext is not MainViewModel { MainMenu.HasUnsavedChanges: true } viewModel)
                return;

            e.Cancel = true;

            if (!await viewModel.MainMenu.ConfirmCloseAsync())
                return;

            Closing -= OnClosing;
            Close();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is ILogical source &&
                source.FindLogicalAncestorOfType<Windows.MainWindowController>(true) != null)
                return;

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }
    }
}
