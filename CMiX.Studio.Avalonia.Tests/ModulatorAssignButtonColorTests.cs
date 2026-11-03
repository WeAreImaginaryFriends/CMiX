using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.Studio.Avalonia.Views.Modulation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Studio.Avalonia.Tests
{
    // The dot of the assign button has one color for each state and each modulator category.
    public class ModulatorAssignButtonColorTests
    {
        private static Color Token(string key) => ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

        private static (Blur Blur, ButtonModulatorAssign Button) Show(params Type[] modulatorTypes)
        {
            var provider = TestServiceProviderFactory.Create();
            var blur = (Blur)provider.GetRequiredService<ControlFactory>().Create(typeof(Blur));
            foreach (var type in modulatorTypes)
                blur.ModulatorManager.AddItem(type);

            var view = new Views.Blur { DataContext = blur };
            new Window { Content = view, Width = 500, Height = 800 }.Show();
            Dispatcher.UIThread.RunJobs();

            var button = view.GetVisualDescendants().OfType<ButtonModulatorAssign>().Single(b => b.DataContext == blur.Strength);
            return (blur, button);
        }

        private static IModulator Bind(Blur blur, Type modulatorType)
        {
            var modulator = blur.ModulatorManager.ManagerData.Items.OfType<IModulator>().Single(m => m.GetType() == modulatorType);
            var output = modulator.Outputs.First(((IModulatorBindable)blur.Strength).CanBind);
            blur.Strength.SetModulatorCommand.Execute(new ModulatorOutputSelection(modulator, output));
            TestServiceProviderFactory.Pump();
            return modulator;
        }

        private static Color? DotColor(ButtonModulatorAssign button) =>
            (button.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Ellipse>().Single().Fill as ISolidColorBrush)?.Color;

        [AvaloniaFact]
        public void Dot_WithNothingAssigned_IsUnassigned()
        {
            var (_, button) = Show(typeof(RandomModulator));

            Assert.Equal(Token("ModulatorUnassignedBrush"), DotColor(button));
        }

        [AvaloniaFact]
        public void Dot_WithAnInternalModulator_IsInternal_AndHighlightedWhenHovered()
        {
            var (blur, button) = Show(typeof(RandomModulator));
            var modulator = Bind(blur, typeof(RandomModulator));

            Assert.Equal(Token("ModulatorInternalBrush"), DotColor(button));

            modulator.IsHovered = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Token("ModulatorInternalHighlightBrush"), DotColor(button));
        }

        [AvaloniaFact]
        public void Dot_WithAnExternalModulator_IsExternal_AndHighlightedWhenHovered()
        {
            var (blur, button) = Show(typeof(RedisFloatModulator));
            var modulator = Bind(blur, typeof(RedisFloatModulator));

            Assert.Equal(Token("ModulatorExternalBrush"), DotColor(button));

            modulator.IsHovered = true;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Token("ModulatorExternalHighlightBrush"), DotColor(button));
        }

        [AvaloniaFact]
        public void Dot_OfTheTwoCategories_HaveDifferentColors()
        {
            Assert.NotEqual(Token("ModulatorInternalBrush"), Token("ModulatorExternalBrush"));
            Assert.NotEqual(Token("ModulatorInternalHighlightBrush"), Token("ModulatorExternalHighlightBrush"));
        }

        [AvaloniaFact]
        public void Dot_AfterUnassign_GoesBackToUnassigned()
        {
            var (blur, button) = Show(typeof(RedisFloatModulator));
            Bind(blur, typeof(RedisFloatModulator));

            blur.Strength.SetModulatorCommand.Execute(null);
            TestServiceProviderFactory.Pump();

            Assert.Equal(Token("ModulatorUnassignedBrush"), DotColor(button));
        }
    }
}
