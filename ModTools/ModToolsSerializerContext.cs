using System.Text.Json.Serialization;
using ModTools.Commands.Banner;
using ModTools.Commands.Manifest;

namespace ModTools;

[JsonSourceGenerationOptions(UseStringEnumConverter = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(BannerConfigFile))]
[JsonSerializable(typeof(SummonBannerOptions))]
[JsonSerializable(typeof(IList<Banner>))]
[JsonSerializable(typeof(Charas))]
[JsonSerializable(typeof(Dragons))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<ManifestAsset>))]
internal sealed partial class ModToolsSerializerContext : JsonSerializerContext { }
