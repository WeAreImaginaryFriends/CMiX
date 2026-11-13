using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.Studio.Avalonia.Views.Managers;
using CMiX.UI.Controls.Collection;
using CMiX.UI.Controls.Number;
using CMiX.UI.Controls.Parts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ViewLISAModulator = CMiX.Studio.Avalonia.Views.LISAModulator;

namespace CMiX.Studio.Avalonia.Tests
{
    // The ranges of a LISAModulator in the window.
    public class LISAModulatorViewTests
    {
        private readonly IServiceProvider _provider = TestServiceProviderFactory.Create();

        private LISAModulator NewModulator()
        {
            var blur = (Blur)_provider.GetRequiredService<ControlFactory>().Create(typeof(Blur));
            blur.ModulatorManager.AddItem(typeof(LISAModulator));
            return (LISAModulator)blur.ModulatorManager.SelectedItem;
        }

        private static ViewLISAModulator Show(LISAModulator modulator)
        {
            var view = new ViewLISAModulator { DataContext = modulator };
            new Window { Content = view, Width = 400, Height = 400 }.Show();
            Dispatcher.UIThread.RunJobs();
            return view;
        }

        private static List<RangeValue> RangeValues(Control view) =>
            view.GetVisualDescendants().OfType<RangeValue>().ToList();

        [AvaloniaFact]
        public void Show_AModulator_HasOneRangeValuePerAxis()
        {
            var view = Show(NewModulator());

            Assert.Equal(2, RangeValues(view).Count);
            Assert.Equal(3, view.GetVisualDescendants().OfType<ContentCaptioned>().Count());
        }

        [AvaloniaFact]
        public void Show_AFreshModulator_ShowsTheDefaults()
        {
            var view = Show(NewModulator());

            var ranges = RangeValues(view);
            Assert.Equal(-1.0, ranges.First().LeftValue);
            Assert.Equal(1.0, ranges.First().RightValue);
            Assert.Equal(1.0, ranges.Last().LeftValue);
            Assert.Equal(-1.0, ranges.Last().RightValue);
        }

        [AvaloniaFact]
        public void SetValue_OnARangeValue_ChangesTheRange()
        {
            var modulator = NewModulator();
            var view = Show(modulator);

            var xRange = RangeValues(view).First();
            xRange.LeftValue = -2.0;
            xRange.RightValue = 2.0;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(-2f, modulator.XRange.Minimum.Value);
            Assert.Equal(2f, modulator.XRange.Maximum.Value);
        }

        [AvaloniaFact]
        public void Build_TheListItemOfAModulator_IsAListItemIcon()
        {
            var templates = new PrefabListItemTemplates();
            var modulator = NewModulator();

            Assert.True(templates.Match(modulator));
            Assert.IsType<ListItemIcon>(templates.Build(modulator));
        }
    }
}
