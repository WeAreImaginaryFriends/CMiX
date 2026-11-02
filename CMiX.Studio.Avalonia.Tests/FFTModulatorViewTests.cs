using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.UI.Controls.Number;
using CMiX.UI.Controls.Parts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ViewFFTModulator = CMiX.Studio.Avalonia.Views.FFTModulator;

namespace CMiX.Studio.Avalonia.Tests
{
    // The track index and the four band ranges of an FFTModulator in the window.
    public class FFTModulatorViewTests
    {
        private readonly IServiceProvider _provider = TestServiceProviderFactory.Create();

        private FFTModulator NewModulator()
        {
            var blur = (Blur)_provider.GetRequiredService<ControlFactory>().Create(typeof(Blur));
            blur.ModulatorManager.AddItem(typeof(FFTModulator));
            return (FFTModulator)blur.ModulatorManager.SelectedItem;
        }

        private static ViewFFTModulator Show(FFTModulator modulator)
        {
            var view = new ViewFFTModulator { DataContext = modulator };
            new Window { Content = view, Width = 400, Height = 400 }.Show();
            Dispatcher.UIThread.RunJobs();
            return view;
        }

        private static List<RangeValue> RangeValues(Control view) =>
            view.GetVisualDescendants().OfType<RangeValue>().ToList();

        [AvaloniaFact]
        public void Show_AModulator_HasOneRangeValuePerBand()
        {
            var view = Show(NewModulator());

            Assert.Equal(4, RangeValues(view).Count);
            Assert.Equal(4, view.GetVisualDescendants().OfType<ContentCaptioned>().Count());
        }

        [AvaloniaFact]
        public void Show_AFreshModulator_ShowsZeroToOneOnEveryBand()
        {
            var view = Show(NewModulator());

            Assert.All(RangeValues(view), range =>
            {
                Assert.Equal(0.0, range.LeftValue);
                Assert.Equal(1.0, range.RightValue);
            });
        }

        [AvaloniaFact]
        public void SetValue_OnTheFirstRangeValue_ChangesTheBassRange()
        {
            var modulator = NewModulator();
            var view = Show(modulator);

            var bassRange = RangeValues(view).First();
            bassRange.LeftValue = -2.0;
            bassRange.RightValue = 2.0;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(-2f, modulator.BassRange.Minimum.Value);
            Assert.Equal(2f, modulator.BassRange.Maximum.Value);
        }

        [AvaloniaFact]
        public void SetValue_OnTheLastRangeValue_ChangesTheHighRange()
        {
            var modulator = NewModulator();
            var view = Show(modulator);

            var highRange = RangeValues(view).Last();
            highRange.LeftValue = 3.0;
            highRange.RightValue = 4.0;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(3f, modulator.HighRange.Minimum.Value);
            Assert.Equal(4f, modulator.HighRange.Maximum.Value);
        }
    }
}
