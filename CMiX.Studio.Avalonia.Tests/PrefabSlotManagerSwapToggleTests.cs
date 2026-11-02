using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Compositing;
using CMiX.Core.Prefabs;
using CMiX.Core.Prefabs.Managers;
using CMiX.Studio.Avalonia.Views.Managers;
using CMiX.UI.Controls.Collection;
using CMiX.UI.Controls.Parts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The toggle of the slot that opens the list of the items of the repository.
    public class PrefabSlotManagerSwapToggleTests
    {
        private static (PrefabSlotManager View, ObservableCollection<IControl> Repository, IServiceProvider Provider) Show()
        {
            var provider = TestServiceProviderFactory.Create();
            var manager = provider.GetRequiredService<PrefabManager>();
            var repository = new ObservableCollection<IControl>();
            var view = new PrefabSlotManager { DataContext = manager, ItemsSource = repository };
            new Window { Content = view, Width = 400, Height = 400 }.Show();
            Dispatcher.UIThread.RunJobs();
            return (view, repository, provider);
        }

        private static ToggleButton SwapToggle(PrefabSlotManager view) => view.FindControl<ToggleButton>("SwapToggle")!;

        [AvaloniaFact]
        public void ThePopupOfTheToggle_HasTheSharedDropdownFrame_AndKeepsItsPosition()
        {
            var (view, repository, provider) = Show();
            repository.Add(provider.GetRequiredService<ControlFactory>().Create(typeof(Entity)));
            Dispatcher.UIThread.RunJobs();

            SwapToggle(view).IsChecked = true;
            Dispatcher.UIThread.RunJobs();

            var popup = view.GetLogicalDescendants().OfType<Popup>().Single();
            Assert.IsType<DropdownFrame>(popup.Child);
            Assert.Equal(-8, popup.HorizontalOffset);
            Assert.Equal(-1 - 8, popup.VerticalOffset);
        }

        [AvaloniaFact]
        public void ThePanelOfTheToggle_StartsAtTheLeftEdgeAndTheBottomEdgeOfTheToggle()
        {
            var (view, repository, provider) = Show();
            view.Margin = new Thickness(100, 40, 0, 0);
            repository.Add(provider.GetRequiredService<ControlFactory>().Create(typeof(Entity)));
            Dispatcher.UIThread.RunJobs();
            var toggle = SwapToggle(view);

            toggle.IsChecked = true;
            Dispatcher.UIThread.RunJobs();

            var popup = view.GetLogicalDescendants().OfType<Popup>().Single();
            var panel = popup.Child!.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_Panel");
            Assert.Equal(toggle.PointToScreen(new Point(0, 0)).X, panel.PointToScreen(new Point(0, 0)).X);
            Assert.Equal(toggle.PointToScreen(new Point(0, toggle.Bounds.Height)).Y - 1, panel.PointToScreen(new Point(0, 0)).Y);
        }

        [AvaloniaFact]
        public void ALongRepository_ScrollsInThePopup()
        {
            var (view, repository, provider) = Show();
            var factory = provider.GetRequiredService<ControlFactory>();
            for (var i = 0; i < 40; i++)
                repository.Add(factory.Create(typeof(Entity)));
            Dispatcher.UIThread.RunJobs();

            SwapToggle(view).IsChecked = true;
            Dispatcher.UIThread.RunJobs();

            var popup = view.GetLogicalDescendants().OfType<Popup>().Single();
            var scroll = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "PART_Scroll");
            Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
            Assert.True(scroll.Viewport.Height <= scroll.MaxHeight);
        }

        [AvaloniaFact]
        public void ThePopup_ShowsTheListWithASearchField()
        {
            var (view, repository, provider) = Show();
            repository.Add(provider.GetRequiredService<ControlFactory>().Create(typeof(Entity)));
            Dispatcher.UIThread.RunJobs();

            SwapToggle(view).IsChecked = true;
            Dispatcher.UIThread.RunJobs();

            var popup = view.GetLogicalDescendants().OfType<Popup>().Single();
            var list = popup.Child!.GetVisualDescendants().OfType<SearchableItemPickerList>().Single();
            Assert.True(list.IsVisible);
            Assert.Single(popup.Child!.GetVisualDescendants().OfType<ItemPickerList>());
        }


        // The user's keys go to the window, the popup, and the field that has the focus in it.
        [AvaloniaFact]
        public void InTheOpenPopup_TheArrowKeysMoveTheHighlight()
        {
            var (view, repository, provider) = Show();
            var factory = provider.GetRequiredService<ControlFactory>();
            for (var i = 0; i < 5; i++)
                repository.Add(factory.Create(typeof(Entity)));
            Dispatcher.UIThread.RunJobs();

            SwapToggle(view).IsChecked = true;
            Dispatcher.UIThread.RunJobs();

            var popup = view.GetLogicalDescendants().OfType<Popup>().Single();
            var list = popup.Child!.GetVisualDescendants().OfType<SearchableItemPickerList>().Single();
            var window = (Window)view.GetVisualRoot()!;
            var focused = TopLevel.GetTopLevel(list)!.FocusManager!.GetFocusedElement();
            var host = TopLevel.GetTopLevel(list)!;

            host.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.True(
                list.HighlightedIndex == 1,
                $"Index {list.HighlightedIndex}. Focus: {focused?.GetType().Name}. Host: {host.GetType().Name}. Window is host: {ReferenceEquals(window, host)}.");
        }

        [AvaloniaFact]
        public void RepositoryWithoutItems_DisablesTheToggle()
        {
            var (view, _, _) = Show();

            Assert.False(SwapToggle(view).IsEnabled);
        }

        [AvaloniaFact]
        public void RepositoryWithAnItem_EnablesTheToggle()
        {
            var (view, repository, provider) = Show();

            repository.Add(provider.GetRequiredService<ControlFactory>().Create(typeof(Entity)));
            Dispatcher.UIThread.RunJobs();

            Assert.True(SwapToggle(view).IsEnabled);
        }

        [AvaloniaFact]
        public void RepositoryEmptiedAgain_DisablesTheToggleAgain()
        {
            var (view, repository, provider) = Show();
            var item = provider.GetRequiredService<ControlFactory>().Create(typeof(Entity));
            repository.Add(item);
            Dispatcher.UIThread.RunJobs();

            repository.Remove(item);
            Dispatcher.UIThread.RunJobs();

            Assert.False(SwapToggle(view).IsEnabled);
        }
    }
}
