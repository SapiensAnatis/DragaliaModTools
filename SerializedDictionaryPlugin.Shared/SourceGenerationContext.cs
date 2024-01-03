using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializableDictionaryPlugin.Shared;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<object, object>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(JsonElement))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
