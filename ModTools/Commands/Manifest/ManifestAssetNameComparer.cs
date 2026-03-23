namespace ModTools.Commands.Manifest;

internal sealed class ManifestAssetNameComparer : IEqualityComparer<ManifestAsset>
{
    public static ManifestAssetNameComparer Instance { get; } = new();

    public bool Equals(ManifestAsset? x, ManifestAsset? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return x.Name == y.Name;
    }

    public int GetHashCode(ManifestAsset obj)
    {
        return obj.Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
