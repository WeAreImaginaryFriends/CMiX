
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using CMiX.Studio.Avalonia.Views.Managers;
using CMiX.UI.Controls.Collection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests.Integration
{
    // The whole flow in the real window: search for an entity in the swap list, pick it with Enter, remove it from the slot,
    // and bring it back with the undo keys of the window.
    public class SwapListUndoTests
    {
        private sealed record Setup(
            Views.MainWindow Window,
            Layer Layer,
            Entity Entity,
            PrefabSlotManager Slot,
            UndoManager Undo);

        private static Setup Show(bool neighbour = false, bool fromRepositoryTab = false)
        {
            var (provider, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();

            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)viewModel.Project.CompositionManager.SelectedItem;

            composition.LayerManager.AddItem(typeof(Layer));
            var first = (Layer)composition.LayerManager.SelectedItem;

            // The entity lives in a slot of another layer, or in the entities of the repository tab.
            var holder = fromRepositoryTab ? viewModel.EntityManager : first.ModelEntityManager;
            holder.AddItem(typeof(Entity));
            var entity = (Entity)holder.SelectedItem;
            entity.PrefabService.Name.Value = "Torus";

            composition.LayerManager.AddItem(typeof(Layer));
            var second = (Layer)composition.LayerManager.SelectedItem;

            // A slot that already holds an entity, as a project in use does.
            if (neighbour)
            {
                second.ModelEntityManager.AddItem(typeof(Entity));
                ((Entity)second.ModelEntityManager.SelectedItem).PrefabService.Name.Value = "Cube";
            }

            var tabControl = TestServiceProviderFactory.MainTabControl(window);
            tabControl.SelectedItem = tabControl.Items.OfType<TabItem>().Single(tab => tab.Content is Views.MainWindowLayersTab);
            TestServiceProviderFactory.Pump();

            var slot = window.GetVisualDescendants().OfType<PrefabSlotManager>()
                .First(manager => ReferenceEquals(manager.ItemsSource, viewModel.ControlRepository.ModelEntities)
                                  && ReferenceEquals(manager.DataContext, second.ModelEntityManager));

            return new Setup(window, second, entity, slot, provider.GetRequiredService<UndoManager>());
        }

        private static void PickWithSearch(Setup setup, string text)
        {
            var toggle = setup.Slot.FindControl<ToggleButton>("SwapToggle")!;
            toggle.IsChecked = true;
            TestServiceProviderFactory.Pump();

            setup.Window.KeyTextInput(text);
            Dispatcher.UIThread.RunJobs();
            setup.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            TestServiceProviderFactory.Pump();
        }

        private static void RemoveFromSlot(Setup setup)
        {
            var manager = setup.Layer.ModelEntityManager;
            manager.RemoveItemCommand.Execute(manager.SelectedItem);
            TestServiceProviderFactory.Pump();
        }

        [AvaloniaFact]
        public void SearchEnterRemoveUndo_BringsTheEntityBack()
        {
            var setup = Show();
            var items = setup.Layer.ModelEntityManager.ManagerData.Items;

            PickWithSearch(setup, "tor");
            Assert.Contains(setup.Entity, items);

            RemoveFromSlot(setup);
            Assert.DoesNotContain(setup.Entity, items);
            Assert.True(setup.Undo.CanUndo, "The removal was not recorded.");

            setup.Window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.Contains(setup.Entity, items);
        }

        private static int Rows(Setup setup)
        {
            var box = setup.Slot.FindControl<CMiX.UI.Controls.ListBoxCMiX>("prefabListBox")!;
            return box.GetVisualDescendants().OfType<ListBoxItem>().Count(item => item.DataContext != null);
        }

        [AvaloniaFact]
        public void TheListOnScreen_FollowsThePickTheRemoveAndTheUndo()
        {
            var setup = Show();
            Assert.Equal(0, Rows(setup));

            PickWithSearch(setup, "tor");
            Assert.Equal(1, Rows(setup));

            RemoveFromSlot(setup);
            Assert.Equal(0, Rows(setup));

            setup.Window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.Equal(1, Rows(setup));
        }

        [AvaloniaFact]
        public void SearchEnterUndoRedo_PutsTheEntityBack()
        {
            var setup = Show();
            var items = setup.Layer.ModelEntityManager.ManagerData.Items;

            PickWithSearch(setup, "tor");
            Assert.Contains(setup.Entity, items);

            setup.Window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();
            Assert.DoesNotContain(setup.Entity, items);

            Assert.True(setup.Undo.CanRedo, "The redo stack is empty after the undo.");

            setup.Window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.Contains(setup.Entity, items);
        }

        [AvaloniaFact]
        public void WithANeighbourInTheSlot_SearchEnterUndoRedo_PutsTheEntityBack()
        {
            var setup = Show(neighbour: true);
            var items = setup.Layer.ModelEntityManager.ManagerData.Items;
            Assert.Single(items);

            PickWithSearch(setup, "tor");
            Assert.Equal(2, items.Count);

            setup.Window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();
            Assert.Single(items);
            Assert.True(setup.Undo.CanRedo, "The redo stack was emptied by something after the undo.");

            setup.Window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.Equal(2, items.Count);
            Assert.Contains(setup.Entity, items);
        }

        [AvaloniaFact]
        public void AnEntityOfTheRepositoryTab_SearchEnterUndoRedo_PutsTheEntityBack()
        {
            var setup = Show(neighbour: true, fromRepositoryTab: true);
            var items = setup.Layer.ModelEntityManager.ManagerData.Items;

            PickWithSearch(setup, "tor");
            Assert.Equal(2, items.Count);
            Assert.Equal(2, Rows(setup));

            setup.Window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();
            Assert.Single(items);
            Assert.Equal(1, Rows(setup));
            Assert.True(setup.Undo.CanRedo, "The redo stack was emptied by something after the undo.");

            setup.Window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            TestServiceProviderFactory.Pump();

            Assert.Equal(2, items.Count);
            Assert.Equal(2, Rows(setup));
            Assert.Contains(setup.Entity, items);
        }

        [AvaloniaFact]
        public void SearchEnterRemoveUndo_WithTheUndoManagerDirectly_BringsTheEntityBack()
        {
            var setup = Show();
            var items = setup.Layer.ModelEntityManager.ManagerData.Items;
            PickWithSearch(setup, "tor");
            RemoveFromSlot(setup);

            setup.Undo.Undo();
            TestServiceProviderFactory.Pump();

            Assert.Contains(setup.Entity, items);
        }
    }
}
