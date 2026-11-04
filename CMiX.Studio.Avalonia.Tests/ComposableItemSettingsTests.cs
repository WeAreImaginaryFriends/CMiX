using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core;
using CMiX.Core.Prefabs;
using CMiX.Studio.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Composition = CMiX.Core.Compositing.Composition;
using Layer = CMiX.Core.Compositing.Layer;

namespace CMiX.Studio.Avalonia.Tests
{
    // The settings of a composition or of a layer are one view. The lists and the inspector show it.
    public class ComposableItemSettingsTests
    {
        private static (Window Window, ComposableItemSettings View, IControl Item) Show(Type itemType)
        {
            var provider = TestServiceProviderFactory.Create();
            var item = provider.GetRequiredService<ControlFactory>().Create(itemType);
            var view = new ComposableItemSettings { DataContext = item };
            var window = new Window { Content = view, Width = 400, Height = 600 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            return (window, view, item);
        }

        [AvaloniaTheory]
        [InlineData(typeof(Layer))]
        [InlineData(typeof(Composition))]
        public void TheView_ShowsTheBlendSettings_AndTheMaskSettingsOnlyForAMask(Type itemType)
        {
            var (_, view, item) = Show(itemType);
            var blend = view.GetVisualDescendants().OfType<ComposableManagerSettings>().Single();
            var mask = view.GetVisualDescendants().OfType<ComposableManagerMaskSettings>().Single();
            Assert.True(blend.IsEffectivelyVisible);
            Assert.False(mask.IsVisible);

            ((dynamic)item).Mask.IsMask.Value = true;
            Dispatcher.UIThread.RunJobs();

            Assert.True(mask.IsVisible);
        }

        [AvaloniaFact]
        public void TheLists_UseThisView_ForTheSelectedItem()
        {
            var (_, window, viewModel) = TestServiceProviderFactory.ShowMainWindow();
            viewModel.Project.CompositionManager.AddItem(typeof(Composition));
            TestServiceProviderFactory.SelectMainTab(window, "Composition");
            TestServiceProviderFactory.Pump();

            var settings = window.GetVisualDescendants().OfType<ComposableItemSettings>().ToList();

            Assert.NotEmpty(settings);
            Assert.All(settings, view => Assert.Same(viewModel.Project.CompositionManager.SelectedItem, view.DataContext));
        }
    }
}
