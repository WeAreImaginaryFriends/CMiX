using CMiX.Core.Modulation;
using CMiX.Core.Modulation.Modulators;
using CMiX.Core.Prefabs;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class DisplaceTests
    {
        [Fact]
        public void ModulatorManager_CanAddRandomModulator()
        {
            var provider = TestServiceProviderFactory.Create();
            var displace = provider.GetRequiredService<Displace>();

            displace.ModulatorManager.AddItem(typeof(RandomModulator));

            Assert.Single(displace.ModulatorManager.ManagerData.Items);
            Assert.IsType<RandomModulator>(displace.ModulatorManager.ManagerData.Items[0]);
        }

        [Fact]
        public void ReorderingModulators_DoesNotUnassignTheMovedOnesBindings()
        {
            var provider = TestServiceProviderFactory.Create();
            var factory = provider.GetRequiredService<ControlFactory>();
            var displace = (Displace)factory.Create(typeof(Displace));

            displace.ModulatorManager.AddItem(typeof(BeatRandomModulator));
            var mod1 = (BeatRandomModulator)displace.ModulatorManager.ManagerData.Items[0];
            displace.ModulatorManager.AddItem(typeof(BeatRandomModulator));
            var mod2 = (BeatRandomModulator)displace.ModulatorManager.ManagerData.Items[1];

            displace.Offset.X.SetModulatorCommand.Execute(new ModulatorOutputSelection(mod1, mod1.Outputs[0]));
            displace.OffsetScale.Y.SetModulatorCommand.Execute(new ModulatorOutputSelection(mod2, mod2.Outputs[0]));

            displace.ModulatorManager.ManagerData.Items.Move(0, 1);

            Assert.Equal(mod1.ID, displace.Offset.X.ModulatorID);
            Assert.Same(mod1, displace.Offset.X.BoundModulator);
            Assert.Equal(mod2.ID, displace.OffsetScale.Y.ModulatorID);
            Assert.Same(mod2, displace.OffsetScale.Y.BoundModulator);
        }

        [Fact]
        public void DeletingAssignedModulator_UnassignsItFromANestedVectorBindable()
        {
            var provider = TestServiceProviderFactory.Create();
            var displace = provider.GetRequiredService<Displace>();
            displace.ModulatorManager.AddItem(typeof(RandomModulator));
            var randomModulator = (RandomModulator)displace.ModulatorManager.ManagerData.Items[0];

            displace.Offset.Y.SetModulatorCommand.Execute(new ModulatorOutputSelection(randomModulator, randomModulator.Outputs[0]));

            displace.ModulatorManager.RemoveItem(randomModulator);

            Assert.Null(displace.Offset.Y.ModulatorID);
            Assert.Null(displace.Offset.Y.BoundModulator);
        }

        [Fact]
        public void Displace_ToModel_FromModel_RoundTripsNestedVectorValueAndLiveBoundModulator()
        {
            var provider = TestServiceProviderFactory.Create();
            var displace = provider.GetRequiredService<Displace>();

            displace.OffsetScale.X.Value = 4f;
            displace.ModulatorManager.AddItem(typeof(RandomModulator));
            var randomModulator = (RandomModulator)displace.ModulatorManager.ManagerData.Items[0];
            displace.OffsetScale.X.SetModulatorCommand.Execute(new ModulatorOutputSelection(randomModulator, randomModulator.Outputs[0]));

            var model = displace.ToModel();

            var provider2 = TestServiceProviderFactory.Create();
            var reloaded = provider2.GetRequiredService<Displace>();
            reloaded.FromModel(model);

            Assert.Equal(4f, reloaded.OffsetScale.X.Value);
            var reloadedRandomModulator = Assert.IsType<RandomModulator>(reloaded.ModulatorManager.ManagerData.Items[0]);
            Assert.Same(reloadedRandomModulator, reloaded.OffsetScale.X.BoundModulator);
        }

        private static (ITextureSource Texture, Displace Filter) TextureWithDisplace(ControlFactory factory)
        {
            var texture = (ITextureSource)factory.Create(typeof(Gradient));
            texture.TextureModifierManager.AddItem(typeof(Displace));
            return (texture, (Displace)texture.TextureModifierManager.ManagerData.Items[0]);
        }

        [Fact]
        public void AddExistingItem_TheOwnTexture_IsRefused()
        {
            var factory = TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>();
            var (texture, filter) = TextureWithDisplace(factory);

            filter.TextureSelector.AddExistingItem(texture);

            Assert.Empty(filter.TextureSelector.ManagerData.Items);
        }

        [Fact]
        public void SelectedItem_TheOwnTexture_IsRefused()
        {
            var factory = TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>();
            var (texture, filter) = TextureWithDisplace(factory);

            filter.TextureSelector.SelectedItem = texture;

            Assert.Empty(filter.TextureSelector.ManagerData.Items);
            Assert.Null(filter.TextureSelector.SelectedItem);
        }

        [Fact]
        public void AddExistingItem_ATextureThatDisplacesWithTheOwner_IsRefused()
        {
            var factory = TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>();
            var (a, filterA) = TextureWithDisplace(factory);
            var (b, filterB) = TextureWithDisplace(factory);

            filterA.TextureSelector.AddExistingItem(b);
            filterB.TextureSelector.AddExistingItem(a);

            Assert.Same(b, Assert.Single(filterA.TextureSelector.ManagerData.Items));
            Assert.Empty(filterB.TextureSelector.ManagerData.Items);
        }

        [Fact]
        public void AddExistingItem_ATextureThatLoopsThroughAThirdOne_IsRefused()
        {
            var factory = TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>();
            var (a, filterA) = TextureWithDisplace(factory);
            var (b, filterB) = TextureWithDisplace(factory);
            var (c, filterC) = TextureWithDisplace(factory);

            filterA.TextureSelector.AddExistingItem(b);
            filterB.TextureSelector.AddExistingItem(c);
            filterC.TextureSelector.AddExistingItem(a);

            Assert.Empty(filterC.TextureSelector.ManagerData.Items);
        }

        [Fact]
        public void AddExistingItem_AnUnrelatedTexture_IsAccepted()
        {
            var factory = TestServiceProviderFactory.Create().GetRequiredService<ControlFactory>();
            var (_, filter) = TextureWithDisplace(factory);
            var other = (ITextureSource)factory.Create(typeof(Gradient));

            filter.TextureSelector.AddExistingItem(other);

            Assert.Same(other, filter.TextureSelector.SelectedItem);
        }
    }
}
