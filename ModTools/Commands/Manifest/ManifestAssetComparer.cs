using System.Diagnostics;
using AssetsTools.NET;

namespace ModTools.Commands.Manifest;

internal sealed class ManifestAssetComparer : IEqualityComparer<AssetTypeValueField>
{
    public static ManifestAssetComparer Instance { get; } = new();

    public bool Equals(AssetTypeValueField? x, AssetTypeValueField? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        AssetTypeValueField xName = x["name"];
        AssetTypeValueField yName = y["name"];
        Debug.Assert(!xName.IsDummy);
        Debug.Assert(!yName.IsDummy);

        return xName.AsString == yName.AsString;
    }

    public int GetHashCode(AssetTypeValueField obj)
    {
        AssetTypeValueField name = obj["name"];

        Debug.Assert(!name.IsDummy);

        return name.AsString.GetHashCode(StringComparison.Ordinal);
    }
}
