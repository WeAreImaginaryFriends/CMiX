using System.Collections.Specialized;
using CMiX.Core.Animations;
using CMiX.Core.Compositing;
using CMiX.Core.Networking;
using CMiX.Core.Networking.Messages;
using CMiX.Core.Persistence;
using CMiX.Core.Prefabs.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // The Redis addresses are part of the project.
    public class ProjectRedisAddressTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDir = new();

        public void Dispose() => _tempDir.Dispose();

        private static Project NewProject() =>
            TestServiceProviderFactory.Create().GetRequiredService<Project>();

        private static void AddAddress(Project project, string text)
        {
            var manager = project.RedisAddressRegistry.AddressManager;
            manager.AddItem(typeof(RedisAddress));
            ((RedisAddress)manager.SelectedItem).Value = text;
        }

        private static IEnumerable<string> Addresses(Project project) =>
            project.RedisAddressRegistry.Addresses.Select(address => address.Value);

        // A project and the messages it sends from now on.
        private static (Project Project, ControlMessenger Messenger, RecordingMessageSender Sender) NewRecordedProject()
        {
            var provider = TestServiceProviderFactory.Create();
            var messenger = provider.GetRequiredService<ControlMessenger>();
            var sender = new RecordingMessageSender();
            messenger.Register(sender);
            var project = provider.GetRequiredService<Project>();
            sender.Sent.Clear();
            return (project, messenger, sender);
        }

        private static ProjectModel ModelWithAddresses(params string[] texts)
        {
            var source = NewProject();
            foreach (var text in texts)
                AddAddress(source, text);
            return (ProjectModel)source.ToModel();
        }

        [Fact]
        public void Open_SendsAnAddForEachAddress()
        {
            var model = ModelWithAddresses("cmix:a", "cmix:b");
            var (project, _, sender) = NewRecordedProject();

            project.Open(model, new List<CompositionModel>());

            var adds = sender.Sent.OfType<MessageAddItem>().ToList();
            Assert.Equal(model.RedisAddressManager.ManagerData.Items.Select(i => i.ID), adds.Select(a => a.Model.ID));
            Assert.All(adds, a => Assert.Equal(ManagerIDs.RedisAddressManager, a.ID));
        }

        [Fact]
        public void ApplyingASnapshot_SendsNothing()
        {
            var model = ModelWithAddresses("cmix:a");
            var (project, messenger, sender) = NewRecordedProject();
            messenger.IsSendingBlocked = true;

            project.ApplySnapshot(model);

            Assert.Empty(sender.Sent);
            Assert.Equal(new[] { "cmix:a" }, Addresses(project));
        }

        [Fact]
        public void SaveThenLoad_KeepsTheAddressesAndTheirIDs()
        {
            var source = NewProject();
            AddAddress(source, "cmix:a");
            AddAddress(source, "cmix:b");
            var path = Path.Combine(_tempDir.Path, "addresses.cmix");

            ProjectSerializer.Save((ProjectModel)source.ToModel(), path);
            var target = NewProject();
            target.Open(ProjectSerializer.Load(path), new List<CompositionModel>());

            Assert.Equal(new[] { "cmix:a", "cmix:b" }, Addresses(target));
            Assert.Equal(source.RedisAddressRegistry.Addresses.Select(a => a.ID),
                         target.RedisAddressRegistry.Addresses.Select(a => a.ID));
        }

        [Fact]
        public void LoadingAFileWithoutAddresses_KeepsTheFixedID()
        {
            var project = NewProject();

            project.Open(new ProjectModel(), new List<CompositionModel>());

            Assert.Equal(ManagerIDs.RedisAddressManager, project.RedisAddressRegistry.AddressManager.ManagerData.ID);
            Assert.Empty(Addresses(project));
        }

        [Fact]
        public void ANewProject_HasNoAddress()
        {
            var project = NewProject();
            AddAddress(project, "cmix:a");

            project.Reset();

            Assert.Empty(Addresses(project));
        }

        [Fact]
        public void ApplyingASnapshot_RestoresTheAddressesBeforeTheCompositions()
        {
            var source = NewProjectWith("cmix:a");
            source.CompositionManager.AddItem(typeof(Composition));
            var snapshot = (ProjectModel)source.ToModel();
            var target = NewProject();
            var addressesWhenACompositionIsAdded = -1;
            target.CompositionManager.ManagerData.Items.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add && addressesWhenACompositionIsAdded < 0)
                    addressesWhenACompositionIsAdded = target.RedisAddressRegistry.Addresses.Count();
            };

            target.ApplySnapshot(snapshot);

            Assert.Equal(1, addressesWhenACompositionIsAdded);
        }

        [Fact]
        public void ApplyingASnapshot_ReplacesTheAddresses()
        {
            var project = NewProject();
            AddAddress(project, "cmix:old");
            var snapshot = (ProjectModel)NewProjectWith("cmix:new").ToModel();

            project.ApplySnapshot(snapshot);

            Assert.Equal(new[] { "cmix:new" }, Addresses(project));
        }

        [Fact]
        public void AddingAnAddress_ChangesTheStateHash()
        {
            var project = NewProject();
            var before = ProjectStateHash.Compute(project);

            AddAddress(project, "cmix:a");

            Assert.NotEqual(before, ProjectStateHash.Compute(project));
        }

        private static Project NewProjectWith(string text)
        {
            var project = NewProject();
            AddAddress(project, text);
            return project;
        }
    }
}
