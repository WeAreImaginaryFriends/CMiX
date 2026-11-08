using System.Text.Json;
using System.Text.Json.Nodes;
using CMiX.Core.Compositing;
using CMiX.Core.Modulation;
using CMiX.Core.Persistence;
using CMiX.Core.Serialization;
using CMiX.Core.Rendering;
using CMiX.Core.Texturing.Filters;
using CMiX.Core.Texturing.Sources;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CMiX.Core.Tests
{
    public class ModulatableValueUpgradeJsonConverterTests
    {
        private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid C = Guid.Parse("33333333-3333-3333-3333-333333333333");

        [Fact]
        public void Read_FlatFloatValue_GivesAModulatableValueWithThatValue()
        {
            var json = "{\"ID\":\"11111111-1111-1111-1111-111111111111\",\"Value\":2.5}";

            var model = JsonSerializer.Deserialize<ModulatableValueModel<float>>(json, ProjectSerializer.Options);

            Assert.Equal(2.5f, model.Value.Value);
            Assert.Equal(A, model.ID);
            Assert.Equal(A, model.Value.ID);
            Assert.Equal("", model.Label);
            Assert.Null(model.ModulatorID.Value);
            Assert.Null(model.BoundOutputName.Value);
        }

        [Fact]
        public void Read_FlatBoolValue_GivesAModulatableValueWithThatValue()
        {
            var json = "{\"ID\":\"11111111-1111-1111-1111-111111111111\",\"Value\":true}";

            var model = JsonSerializer.Deserialize<ModulatableValueModel<bool>>(json, ProjectSerializer.Options);

            Assert.True(model.Value.Value);
            Assert.Equal(A, model.ID);
            Assert.Equal(A, model.Value.ID);
        }

        [Fact]
        public void Read_FlatIntValue_GivesAModulatableValueWithThatValue()
        {
            var json = "{\"ID\":\"11111111-1111-1111-1111-111111111111\",\"Value\":3}";

            var model = JsonSerializer.Deserialize<ModulatableValueModel<int>>(json, ProjectSerializer.Options);

            Assert.Equal(3, model.Value.Value);
            Assert.Equal(A, model.ID);
            Assert.Equal(A, model.Value.ID);
        }

        [Fact]
        public void Read_FlatValueWithoutID_GivesANewID()
        {
            var model = JsonSerializer.Deserialize<ModulatableValueModel<float>>("{\"Value\":1.5}", ProjectSerializer.Options);

            Assert.NotEqual(Guid.Empty, model.ID);
            Assert.Equal(1.5f, model.Value.Value);
        }

        [Fact]
        public void Read_NestedValue_GivesTheSameModelAsSerialized()
        {
            var model = ModulatableValueModel<float>.Of("Gamma", 2.2f) with
            {
                ModulatorID = new(Guid.NewGuid()),
                BoundOutputName = new("Out")
            };

            var json = JsonSerializer.Serialize(model, ProjectSerializer.Options);
            var loaded = JsonSerializer.Deserialize<ModulatableValueModel<float>>(json, ProjectSerializer.Options);

            Assert.Equal(model, loaded);
        }

        [Fact]
        public void Read_FlatVector2_GivesAModulatableVector2WithBothCells()
        {
            var json = "{\"ID\":\"11111111-1111-1111-1111-111111111111\","
                + "\"X\":{\"ID\":\"22222222-2222-2222-2222-222222222222\",\"Value\":8},"
                + "\"Y\":{\"ID\":\"33333333-3333-3333-3333-333333333333\",\"Value\":4}}";

            var model = JsonSerializer.Deserialize<ModulatableVector2Model>(json, ProjectSerializer.Options);

            Assert.Equal(8f, model.X.Value.Value);
            Assert.Equal(4f, model.Y.Value.Value);
            Assert.Equal(B, model.X.ID);
            Assert.Equal(C, model.Y.ID);
            Assert.Equal(A, model.ID);
        }

        [Fact]
        public void Write_AModelWithModulatableValues_GivesTheSameTextAsWithoutTheConverter()
        {
            var without = new JsonSerializerOptions(ProjectSerializer.Options);
            without.Converters.Remove(without.Converters.OfType<ModulatableValueUpgradeJsonConverter>().Single());
            var model = (IControlModel)new HSCBModel();

            var withText = JsonSerializer.Serialize(model, ProjectSerializer.Options);
            var withoutText = JsonSerializer.Serialize(model, without);

            Assert.Equal(withoutText, withText);
        }

        [Fact]
        public void Read_GradientWithFlatGamma_KeepsTheGammaValue()
        {
            var node = JsonSerializer.SerializeToNode(new GradientModel(), ProjectSerializer.Options);
            node["Gamma"] = JsonNode.Parse("{\"ID\":\"11111111-1111-1111-1111-111111111111\",\"Value\":1.8}");

            var model = node.Deserialize<GradientModel>(ProjectSerializer.Options);

            Assert.Equal(1.8f, model.Gamma.Value.Value);
            Assert.Equal(A, model.Gamma.ID);
        }

        [Fact]
        public void Load_AFileWithAFlatGamma_OpensAndSavesTheNewShape()
        {
            using var directory = new TempDirectoryFixture();
            var path = Path.Combine(directory.Path, "flat-gamma.cmix");
            ProjectSerializer.Save(BuildProjectWithAGradient(), path);
            var root = JsonNode.Parse(File.ReadAllText(path));
            var gradient = FindObjectWithKey(root, "Gamma");
            gradient["Gamma"] = JsonNode.Parse("{\"ID\":\"11111111-1111-1111-1111-111111111111\",\"Value\":1.8}");
            File.WriteAllText(path, root.ToJsonString());

            var loaded = ProjectSerializer.Load(path);
            ProjectSerializer.Save(loaded, path);

            var saved = File.ReadAllText(path);
            var gamma = FindObjectWithKey(JsonNode.Parse(saved), "Gamma")["Gamma"];
            Assert.Equal(1.8f, gamma["Value"]["Value"].GetValue<float>());
            Assert.Matches(@"""Gamma"": \{[^}]*""Value"": \{", saved);
        }

        private static ProjectModel BuildProjectWithAGradient()
        {
            var project = TestServiceProviderFactory.Create().GetRequiredService<Project>();
            project.CompositionManager.AddItem(typeof(Composition));
            var composition = (Composition)project.CompositionManager.SelectedItem;
            composition.LayerManager.AddItem(typeof(Layer));
            var layer = (Layer)composition.LayerManager.SelectedItem;
            layer.ModelEntityManager.AddItem(typeof(Entity));
            var entity = (Entity)layer.ModelEntityManager.SelectedItem;
            entity.Material.DiffuseTexture.TextureManager.AddItem(typeof(Gradient));
            return (ProjectModel)project.ToModel();
        }

        private static JsonObject FindObjectWithKey(JsonNode node, string key)
        {
            if (node is JsonObject obj)
            {
                if (obj.ContainsKey(key))
                    return obj;
                foreach (var child in obj)
                    if (child.Value != null && FindObjectWithKey(child.Value, key) is { } found)
                        return found;
            }
            else if (node is JsonArray array)
            {
                foreach (var child in array)
                    if (child != null && FindObjectWithKey(child, key) is { } found)
                        return found;
            }
            return null;
        }

        [Fact]
        public void Options_Converters_HoldOneUpgradeConverter()
        {
            Assert.Single(ProjectSerializer.Options.Converters.OfType<ModulatableValueUpgradeJsonConverter>());
        }
    }
}
