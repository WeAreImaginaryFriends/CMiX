using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CMiX.Core.BaseControls;
using CMiX.Core.Modulation;

namespace CMiX.Core.Serialization
{
    // Reads a plain GenericValueModel in the place of a ModulatableValueModel. Project files from before
    // the modulatable Gamma, Frequency, Contrast and Cell Count store that shape. Remove this class when no such file
    // needs to open. See README, Project files.
    public class ModulatableValueUpgradeJsonConverter : JsonConverterFactory
    {
        private readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _plain = new();

        public override bool CanConvert(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ModulatableValueModel<>);

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(Converter<>).MakeGenericType(type.GetGenericArguments()[0]), this);

        // The options without this converter. They prevent recursion.
        private JsonSerializerOptions Plain(JsonSerializerOptions options) =>
            _plain.GetValue(options, o =>
            {
                var plain = new JsonSerializerOptions(o);
                plain.Converters.Remove(this);
                return plain;
            });

        private sealed class Converter<T> : JsonConverter<ModulatableValueModel<T>>
        {
            private readonly ModulatableValueUpgradeJsonConverter _factory;

            public Converter(ModulatableValueUpgradeJsonConverter factory) => _factory = factory;

            public override ModulatableValueModel<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                var plain = _factory.Plain(options);

                // T is a scalar (float, int, bool) in every use, so a flat shape never looks nested.
                var flat = root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("Value", out var value)
                    && value.ValueKind != JsonValueKind.Object;

                if (!flat)
                    return root.Deserialize<ModulatableValueModel<T>>(plain);

                var inner = root.Deserialize<GenericValueModel<T>>(plain);
                return new ModulatableValueModel<T> { ID = inner.ID, Value = inner };
            }

            public override void Write(Utf8JsonWriter writer, ModulatableValueModel<T> value, JsonSerializerOptions options) =>
                JsonSerializer.Serialize(writer, value, _factory.Plain(options));
        }
    }
}
