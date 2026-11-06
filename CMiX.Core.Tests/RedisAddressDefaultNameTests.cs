using CMiX.Core.Compositing;
using CMiX.Core.Undo;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    // A new Redis address has a name, so its row in the list has a text to edit.
    public class RedisAddressDefaultNameTests
    {
        private static (Project Project, UndoManager Undo) NewProject()
        {
            var provider = TestServiceProviderFactory.Create();
            return (provider.GetRequiredService<Project>(), provider.GetRequiredService<UndoManager>());
        }

        private static List<string> Names(Project project) =>
            project.RedisAddressRegistry.Addresses.Select(address => address.Value).ToList();

        [Fact]
        public void Add_AnAddress_NamesItAddressWithANumber()
        {
            var (project, _) = NewProject();

            project.RedisAddressRegistry.AddAddress();
            project.RedisAddressRegistry.AddAddress();

            Assert.Equal(new[] { "Address.001", "Address.002" }, Names(project));
        }

        [Fact]
        public void Add_AnAddressAfterOthers_TakesTheFirstFreeNumber()
        {
            var (project, _) = NewProject();
            project.RedisAddressRegistry.AddAddress();
            project.RedisAddressRegistry.AddAddress();
            project.RedisAddressRegistry.AddAddress();
            project.RedisAddressRegistry.RemoveAddress(project.RedisAddressRegistry.Addresses.ElementAt(1));

            project.RedisAddressRegistry.AddAddress();

            Assert.Equal(new[] { "Address.001", "Address.003", "Address.002" }, Names(project));
        }

        [Fact]
        public void Add_AnAddress_IsOneUndoStepWithItsName()
        {
            var (project, undo) = NewProject();
            undo.Clear();

            project.RedisAddressRegistry.AddAddress();
            undo.Undo();

            Assert.Empty(Names(project));
        }
    }
}
