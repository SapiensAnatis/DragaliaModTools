using AssetsTools.NET;
using ModTools.Shared;
using SerializableDictionaryPlugin.Shared;

namespace ModTools.Commands;

internal sealed class ImportDictionaryCommand
{
    /// <summary>
    /// Import a single serialized dictionary over an asset.
    /// </summary>
    /// <param name="assetBundlePath">The path of the bundle to import into.</param>
    /// <param name="assetName">--asset|-a The asset to import over.</param>
    /// <param name="dictionaryPath">--dictionary|-d The path to the JSON dictionary file to import.</param>
    /// <param name="outputPath">--output|-o The desired output path.</param>
    /// <param name="inplace">-i Whether to write to the input path in-place. If specified, ignores --output / -o.</param>
    [Command("import")]
    public void Command(
        [Argument] string assetBundlePath,
        string assetName,
        string dictionaryPath,
        string outputPath,
        bool inplace
    )
    {
        using AssetBundleHelper bundleHelper = AssetBundleHelper.FromPath(assetBundlePath);

        AssetTypeValueField field = bundleHelper.GetBaseField(assetName);

        ConsoleApp.Log($"Importing file {dictionaryPath} over asset {assetName}");
        SerializableDictionaryHelper.UpdateFromFile(field, dictionaryPath);

        bundleHelper.UpdateBaseField(assetName, field);

        FileInfo outputFileInfo = new FileInfo(inplace ? assetBundlePath : outputPath);

        ConsoleApp.Log($"Writing output to {outputFileInfo.FullName}");

        using FileStream fs = outputFileInfo.OpenWrite();
        bundleHelper.Write(fs);
    }
}
