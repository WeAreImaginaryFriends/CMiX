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
using CMiX.Studio.Avalonia.ViewModels.Tree;
using CMiX.Studio.Avalonia.Views;
using CMiX.UI.Controls.String;
using CMiX.UI.Panels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;
using Project = CMiX.Core.Compositing.Project;

namespace CMiX.Studio.Avalonia.Tests
{
    // The keys of the project tree. The arrow keys move the soft selection only. The key bindings come from the shortcut map.
    public class ProjectTreeKeyboardTests : IDisposable
    {
        private Window? window;

        public void Dispose() => window?.Close();

        private sealed record Screen(ProjectTreeView View, ProjectTree Tree, UndoManager Undo, Composition Composition, ShortcutMap Shortcuts);

        private void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(60);
            window!.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        // One composition with three layers. The first layer is selected, the undo history is empty.
        private Screen Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var project = provider.GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.ManagerData.Items[0];
            for (var i = 0; i < 3; i++)
                composition.LayerManager.AddItem(typeof(Layer));

            var undo = provider.GetRequiredService<UndoManager>();
            var shortcuts = provider.GetRequiredService<ShortcutMap>();
            var tree = new ProjectTree(project, undo, null, shortcuts);
            var view = new ProjectTreeView { DataContext = tree };
            window = new Window { Content = view, Width = 500, Height = 700 };
            window.Show();
            foreach (var panel in view.GetVisualDescendants().OfType<CollapsePanel>())
                panel.Duration = TimeSpan.Zero;
            tree.Roots[0].IsExpanded = true;
            foreach (var layer in tree.Roots[0].Children)
                layer.IsExpanded = false;
            Settle();

            tree.SelectedNode = tree.Roots[0].Children[0];
            Settle();
            ItemOf(view, tree.Roots[0].Children[0]).Focus();
            Settle();
            undo.Clear();
            return new Screen(view, tree, undo, composition, shortcuts);
        }

        private static TreeViewItem ItemOf(ProjectTreeView view, TreeNode node) =>
            view.GetVisualDescendants().OfType<TreeViewItem>().First(item => ReferenceEquals(item.DataContext, node));

        private static Border Part(ProjectTreeView view, TreeNode node, string name) =>
            ItemOf(view, node).GetVisualDescendants().OfType<Border>().First(border => border.Name == name);

        private void Press(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            window!.KeyPressQwerty(key, modifiers);
            Settle();
        }

        [AvaloniaFact]
        public void ArrowDown_MovesTheSoftSelection_WithNoUndoStep()
        {
            var screen = Show();
            var first = screen.Tree.Roots[0].Children[0];
            var second = screen.Tree.Roots[0].Children[1];
            var selectedLayer = screen.Composition.LayerManager.SelectedItem;

            Press(PhysicalKey.ArrowDown);

            Assert.Same(second, screen.Tree.SoftSelectedNode);
            Assert.Same(first, screen.Tree.SelectedNode);
            Assert.Same(selectedLayer, screen.Composition.LayerManager.SelectedItem);
            Assert.False(screen.Undo.CanUndo);
        }

        [AvaloniaFact]
        public void Enter_SelectsTheChain()
        {
            var screen = Show();
            var second = screen.Tree.Roots[0].Children[1];
            Press(PhysicalKey.ArrowDown);

            Press(PhysicalKey.Enter);

            Assert.Same(second, screen.Tree.SelectedNode);
            Assert.Equal(SelectionOrigin.Keyboard, screen.Tree.Origin);
            Assert.Same(second.Item, screen.Composition.LayerManager.SelectedItem);
        }

        [AvaloniaFact]
        public void F2_StartsTheRename()
        {
            var screen = Show();
            var first = screen.Tree.Roots[0].Children[0];
            var box = Part(screen.View, first, "RowBorder").GetVisualDescendants().OfType<TextBoxEditable>().First();

            Press(PhysicalKey.F2);

            Assert.True(box.IsEditing);
        }

        [AvaloniaFact]
        public void Delete_RemovesTheSelectedItem()
        {
            var screen = Show();
            var first = screen.Tree.Roots[0].Children[0];

            Press(PhysicalKey.Delete);

            Assert.DoesNotContain(first.Item, screen.Composition.LayerManager.ManagerData.Items);
            Assert.Equal(2, screen.Composition.LayerManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void Delete_InANameBoxThatIsEdited_RemovesNoItem()
        {
            var screen = Show();
            Press(PhysicalKey.F2);
            Press(PhysicalKey.End);

            Press(PhysicalKey.Delete);

            Assert.Equal(3, screen.Composition.LayerManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void KeyBindings_WhileANameBoxIsEdited_ArePaused()
        {
            var screen = Show();

            Press(PhysicalKey.F2);
            Assert.Empty(screen.View.KeyBindings);

            Press(PhysicalKey.Escape);
            ItemOf(screen.View, screen.Tree.Roots[0].Children[0]).Focus();
            Settle();
            Assert.Equal(6, screen.View.KeyBindings.Count);
        }

        [AvaloniaFact]
        public void TreeKeyBinding_AfterAChangeOfTheMap_UsesTheNewGesture()
        {
            var screen = Show();
            screen.Shortcuts.Set("Tree.Remove", new KeyGesture(Key.R, KeyModifiers.Control));
            Settle();

            Press(PhysicalKey.Delete);
            Assert.Equal(3, screen.Composition.LayerManager.ManagerData.Items.Count);

            Press(PhysicalKey.R, RawInputModifiers.Control);
            Assert.Equal(2, screen.Composition.LayerManager.ManagerData.Items.Count);
        }

        [AvaloniaFact]
        public void TreeKeyBindings_HoldTheTreeCommands()
        {
            var screen = Show();
            var tree = screen.Tree;
            var bindings = screen.View.KeyBindings;

            KeyBinding For(string name) => bindings.Single(binding => Equals(binding.Gesture, screen.Shortcuts[name]));

            Assert.Same(tree.RenameSelectedCommand, For("Tree.Rename").Command);
            Assert.Same(tree.RemoveItemCommand, For("Tree.Remove").Command);
            Assert.Same(tree.DuplicateItemCommand, For("Tree.Duplicate").Command);
            Assert.Same(tree.MoveItemUpCommand, For("Tree.MoveUp").Command);
            Assert.Same(tree.MoveItemDownCommand, For("Tree.MoveDown").Command);
            Assert.Same(tree.OpenSelectedCommand, For("Tree.Open").Command);
        }

        [AvaloniaFact]
        public void FocusFrame_OfTheSoftSelectedRow_IsAOnePixelBorderInTheSelectionColor()
        {
            var screen = Show();
            var second = screen.Tree.Roots[0].Children[1];

            Press(PhysicalKey.ArrowDown);

            var frame = Part(screen.View, second, "FocusFrame");
            var selection = Part(screen.View, second, "SelectionFrame");
            Assert.True(frame.IsVisible);
            Assert.False(selection.IsVisible);
            Assert.Equal(new Thickness(1), frame.BorderThickness);
            Assert.Equal(((ISolidColorBrush)selection.BorderBrush!).Color, ((ISolidColorBrush)frame.BorderBrush!).Color);
            Assert.False(Part(screen.View, screen.Tree.Roots[0].Children[0], "FocusFrame").IsVisible);
        }
    }
}
