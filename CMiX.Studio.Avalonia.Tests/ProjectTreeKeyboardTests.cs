using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Services;
using CMiX.Studio.Avalonia.Tests.Support;
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.String;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static CMiX.Studio.Avalonia.Tests.Support.TreeFixtures;
using static CMiX.Studio.Avalonia.Tests.Support.TreeTestScreen;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The keys of the project tree. The arrow keys move the soft selection only. The key bindings come from the shortcut map.
    public class ProjectTreeKeyboardTests : IDisposable
    {
        private Window? window;

        public void Dispose()
        {
            foreach (var view in window?.GetVisualDescendants().OfType<ProjectTreeView>() ?? Enumerable.Empty<ProjectTreeView>())
                view.MenuAddPanel.Close();

            window?.Close();
        }

        private sealed record Screen(ProjectTreeView View, ProjectTree Tree, UndoManager Undo, Composition Composition, ShortcutMap Shortcuts);

        private void Settle() => TreeTestScreen.Settle(window!);

        // One composition with three layers. The first layer is selected, the undo history is empty.
        private Screen Show()
        {
            var scene = CreateScene([3], withShortcuts: true);
            var (tree, undo, composition) = (scene.Tree, scene.Undo, scene.A);
            var shortcuts = scene.Provider.GetRequiredService<ShortcutMap>();
            var screen = TreeTestScreen.Show(tree, 500, 700, settle: false);
            window = screen.Window;
            var view = screen.View;
            tree.CompositionNodes()[0].IsExpanded = true;
            tree.CompositionNodes()[0].Children[0].IsExpanded = true;
            foreach (var layer in tree.CompositionNodes()[0].Children[0].Children)
                layer.IsExpanded = false;
            Settle();

            tree.SelectedNode = tree.CompositionNodes()[0].Children[0].Children[0];
            Settle();
            ItemOf(view, tree.CompositionNodes()[0].Children[0].Children[0]).Focus();
            Settle();
            undo.Clear();
            return new Screen(view, tree, undo, composition, shortcuts);
        }


        private void Press(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            window!.KeyPressQwerty(key, modifiers);
            Settle();
        }

        [AvaloniaFact]
        public void ArrowDown_MovesTheSoftSelection_WithNoUndoStep()
        {
            var screen = Show();
            var first = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            var second = screen.Tree.CompositionNodes()[0].Children[0].Children[1];
            var selectedLayer = screen.Composition.LayerManager.SelectedItem;

            Press(PhysicalKey.ArrowDown);

            Assert.Same(second, screen.Tree.SoftSelectedNode);
            Assert.Same(first, screen.Tree.SelectedNode);
            Assert.Same(selectedLayer, screen.Composition.LayerManager.SelectedItem);
            Assert.False(screen.Undo.CanUndo);
        }

        [AvaloniaFact]
        public void Press_Enter_SelectsTheChain()
        {
            var screen = Show();
            var second = screen.Tree.CompositionNodes()[0].Children[0].Children[1];
            Press(PhysicalKey.ArrowDown);

            Press(PhysicalKey.Enter);

            Assert.Same(second, screen.Tree.SelectedNode);
            Assert.Equal(SelectionOrigin.Keyboard, screen.Tree.Origin);
            Assert.Same(second.Item, screen.Composition.LayerManager.SelectedItem);
        }

        // The open state of every row of the tree.
        private static bool[] OpenStates(ProjectTree tree) => All(tree.Roots).Select(node => node.IsExpanded).ToArray();

        // The keys of the tree view of Avalonia that open or close rows. The focused row is open for the keys that close
        // and closed for the keys that open, so a key that goes through changes it.
        [AvaloniaTheory]
        [InlineData(PhysicalKey.NumPadAdd, false)]
        [InlineData(PhysicalKey.NumPadMultiply, false)]
        [InlineData(PhysicalKey.NumPadSubtract, true)]
        [InlineData(PhysicalKey.NumPadDivide, true)]
        public void Press_ATreeViewKey_OpensOrClosesNoRow(PhysicalKey key, bool open)
        {
            var screen = Show();
            var layer = screen.Tree.CompositionNodes()[0].Children[0].Children[0];
            Assert.NotEmpty(layer.Children);
            layer.IsExpanded = open;
            Settle();
            ItemOf(screen.View, layer).Focus();
            Settle();
            var before = OpenStates(screen.Tree);

            Press(key);

            Assert.Equal(before, OpenStates(screen.Tree));
            Assert.Equal(open, layer.IsExpanded);
        }

        [AvaloniaFact]
        public void KeyBindings_WhileANameBoxIsEdited_ArePaused()
        {
            var screen = Show();

            // The key of the rename is a binding of the main window (see TreeRowKeysTests), so the test starts the rename itself.
            screen.Tree.RenameSelectedCommand.Execute(null);
            Settle();
            Assert.Empty(screen.View.KeyBindings);

            Press(PhysicalKey.Escape);
            ItemOf(screen.View, screen.Tree.CompositionNodes()[0].Children[0].Children[0]).Focus();
            Settle();
            Assert.Single(screen.View.KeyBindings);
        }

        [AvaloniaFact]
        public void FocusFrame_OfTheSoftSelectedRow_IsAOnePixelBorderInTheSelectionColor()
        {
            var screen = Show();
            var second = screen.Tree.CompositionNodes()[0].Children[0].Children[1];

            Press(PhysicalKey.ArrowDown);

            var frame = Part(screen.View, second, "FocusFrame");
            var selection = Part(screen.View, second, "SelectionFrame");
            Assert.True(frame.IsVisible);
            Assert.False(selection.IsVisible);
            Assert.Equal(new Thickness(1), frame.BorderThickness);
            Assert.Equal(((ISolidColorBrush)selection.BorderBrush!).Color, ((ISolidColorBrush)frame.BorderBrush!).Color);
            Assert.False(Part(screen.View, screen.Tree.CompositionNodes()[0].Children[0].Children[0], "FocusFrame").IsVisible);
        }
    }
}
