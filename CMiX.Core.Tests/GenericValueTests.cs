using CMiX.Core.BaseControls;
using CMiX.Core.Networking;
using CMiX.Core.Networking.Messages;
using CMiX.Core.Undo;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class GenericValueTests
    {
        [Fact]
        public void SetDefaults_CopiesTheValueToOriginalValue()
        {
            var provider = TestServiceProviderFactory.Create();
            var value = provider.GetRequiredService<GenericValue<int>>();

            value.Value = 5;
            value.SetDefaults();

            Assert.Equal(5, value.Value);
            Assert.Equal(5, value.OriginalValue);
        }

        [Fact]
        public void Reset_AfterFromModel_ReturnsToTheDefaultValue_NotTheFromModelValue()
        {
            var provider = TestServiceProviderFactory.Create();
            var value = provider.GetRequiredService<GenericValue<int>>();

            value.Value = 5;
            value.SetDefaults();
            value.FromModel(new GenericValueModel<int>(9));
            value.Reset();

            Assert.Equal(5, value.Value);
        }
    

        private static GenericValue<bool> CreateActiveBool(IServiceProvider provider)
        {
            var control = provider.GetRequiredService<GenericValue<bool>>();
            provider.GetRequiredService<ControlActivationService>().ActivateAll();
            return control;
        }

        [Fact]
        public void Value_OfAMomentaryValue_WritesNoUndoStep()
        {
            var provider = TestServiceProviderFactory.Create();
            var undoManager = provider.GetRequiredService<UndoManager>();
            var control = CreateActiveBool(provider);
            control.IsMomentary = true;

            control.Value = true;
            control.Value = false;

            Assert.False(undoManager.CanUndo);
        }

        [Fact]
        public void Value_OfAMomentaryValue_StillSendsTheWireMessage()
        {
            var provider = TestServiceProviderFactory.Create();
            var messenger = provider.GetRequiredService<ControlMessenger>();
            var recorder = new CountingSender();
            messenger.Register(recorder);
            var control = CreateActiveBool(provider);
            control.IsMomentary = true;

            control.Value = true;
            control.Value = false;

            messenger.Unregister(recorder);

            Assert.Equal(2, recorder.Count);
        }

        [Fact]
        public void Value_OfANormalValue_StillWritesAnUndoStep()
        {
            var provider = TestServiceProviderFactory.Create();
            var undoManager = provider.GetRequiredService<UndoManager>();
            var control = CreateActiveBool(provider);

            control.Value = true;

            Assert.True(undoManager.CanUndo);
        }

        private sealed class CountingSender : IMessageSender
        {
            public int Count { get; private set; }

            public void SendMessage(IMessage message)
            {
                if (message is MessageValueChanged) Count++;
            }
        }
    }
}
