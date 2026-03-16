using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModTools.Shared.SerializableDictionary;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<object, object>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class SerializableDictionaryContext : JsonSerializerContext { }
