using AssetsTools.NET;
using ModTools.Shared;

namespace ModTools.Commands;

internal sealed class DumpAssetCommand
{
    /// <summary>
    /// Dumps a JSON object representing an asset's fields to standard output.
    /// </summary>
    /// <param name="assetPath">The path of the asset bundle to load.</param>
    /// <param name="assetName">The name of the asset within the bundle to dump.</param>
    /// <param name="decrypt">Whether the asset bundle being loaded should be decrypted.</param>
    [Command("dump-asset")]
    public void DumpAsset(
        [Argument] string assetPath,
        [Argument] string assetName,
        bool decrypt = false
    )
    {
        using AssetBundleHelper helper = decrypt
            ? AssetBundleHelper.FromPathEncrypted(assetPath)
            : AssetBundleHelper.FromPath(assetPath);

        AssetTypeValueField field = helper.GetBaseField(assetName);

        using Stream stdout = Console.OpenStandardOutput();
        AssetSerializer.Serialize(stdout, field);
    }
}
