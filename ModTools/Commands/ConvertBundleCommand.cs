using ModTools.Shared;

namespace ModTools.Commands;

internal sealed class ConvertBundleCommand
{
    /// <summary>
    /// Converts an Android asset bundle to iOS.
    /// </summary>
    /// <param name="assetBundlePath">The path to the bundle to convert.</param>
    /// <param name="outputPath">-o, The desired output path for the converted bundle.</param>
    [Command("convert")]
    public void Command([Argument] string assetBundlePath, string outputPath)
    {
        using AssetBundleHelper bundleHelper = AssetBundleHelper.FromPath(assetBundlePath);
        FileInfo outputFileInfo = new(outputPath);

        BundleConversionHelper.ConvertToIos(bundleHelper, outputFileInfo);
    }
}
