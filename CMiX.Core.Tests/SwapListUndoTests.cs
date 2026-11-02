using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Core.Undo;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // An item of the repository is added to a slot with the swap list, removed from the slot, and brought back with undo.
    public class SwapListUndoTests
    {
        private static (UndoManager Undo, PrefabManager Slot, Entity Shared) Setup()
        {
            var provider = TestServiceProviderFactory.Create();
            var undo = provider.GetRequiredService<UndoManager>();
            var compositions = provider.GetRequiredService<PrefabManager>();

            compositions.AddItem(typeof(Composition));
            var composition = (Composition)compositions.SelectedItem;

            composition.LayerManager.AddItem(typeof(Layer));
            var first = (Layer)composition.LayerManager.SelectedItem;
            composition.LayerManager.AddItem(typeof(Layer));
            var second = (Layer)composition.LayerManager.SelectedItem;

            first.ModelEntityManager.AddItem(typeof(Entity));
            var shared = (Entity)first.ModelEntityManager.SelectedItem;

            return (undo, second.ModelEntityManager, shared);
        }

        [Fact]
        public void AddFromTheRepository_ThenRemove_ThenUndo_BringsTheItemBack()
        {
            var (undo, slot, shared) = Setup();

            slot.AddExistingItem(shared);
            Assert.Contains(shared, slot.ManagerData.Items);

            slot.RemoveItem(shared);
            Assert.DoesNotContain(shared, slot.ManagerData.Items);

            undo.Undo();

            Assert.Contains(shared, slot.ManagerData.Items);
        }

        [Fact]
        public void AddFromTheRepository_ThenRemove_ThenUndoAndRedo_RemovesItAgain()
        {
            var (undo, slot, shared) = Setup();
            slot.AddExistingItem(shared);
            slot.RemoveItem(shared);

            undo.Undo();
            undo.Redo();

            Assert.DoesNotContain(shared, slot.ManagerData.Items);
        }

        [Fact]
        public void AddFromTheRepository_ThenUndo_TakesTheItemOutAgain()
        {
            var (undo, slot, shared) = Setup();
            slot.AddExistingItem(shared);

            undo.Undo();

            Assert.DoesNotContain(shared, slot.ManagerData.Items);
        }
    }
}
