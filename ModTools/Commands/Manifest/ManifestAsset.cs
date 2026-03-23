namespace ModTools.Commands.Manifest;

internal sealed record ManifestAsset
{
    public required string Name { get; init; }

    public required string Hash { get; init; }

    public List<string>? Dependencies { get; init; }

    public List<string>? Assets { get; init; }

    public long Size { get; init; }

    public int Group { get; init; }
}
