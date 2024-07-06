using AssetsTools.NET;
using ModTools.Shared;
using SerializableDictionaryPlugin.Shared;

namespace ModTools.Commands;

internal sealed class ImportMultipleDictionaryCommand
{
    /// <summary>
    /// Import a directory of serializable dictionary files into an asset bundle.
    /// </summary>
    /// <param name="assetBundlePath">The path to the asset bundle to open.</param>
    /// <param name="directory">-d, The path to the directory containing the dictionary JSON files to import.</param>
    /// <param name="outputPath">--output|-o, The path to write the output asset to.</param>
    [Command("import-multiple")]
    public void Command([Argument] string assetBundlePath, string directory, string outputPath)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(directory);
        FileInfo outputFileInfo = new FileInfo(outputPath);

        using AssetBundleHelper bundleHelper = AssetBundleHelper.FromPath(assetBundlePath);

        foreach (FileInfo file in directoryInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly))
        {
            string assetName = Path.GetFileNameWithoutExtension(file.Name);
            ConsoleApp.Log($"Importing file {file.Name} over asset {assetName}");

            AssetTypeValueField field = bundleHelper.GetBaseField(assetName);

            SerializableDictionaryHelper.UpdateFromFile(field, file.FullName);

            bundleHelper.UpdateBaseField(assetName, field);
        }

        ConsoleApp.Log($"Writing output to {outputPath}");
        using FileStream fs = outputFileInfo.OpenWrite();
        bundleHelper.Write(fs);
    }
}
