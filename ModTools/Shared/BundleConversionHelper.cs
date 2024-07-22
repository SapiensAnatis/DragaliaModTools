using AssetsTools.NET.Extra;

namespace ModTools.Shared;

internal static class BundleConversionHelper
{
    public static void ConvertToIos(FileInfo input, FileInfo output)
    {
        using AssetBundleHelper bundleHelper = AssetBundleHelper.FromPath(input.FullName);

        ConvertToIos(bundleHelper, output);
    }

    internal static void ConvertToIos(AssetBundleHelper bundleHelper, FileInfo output)
    {
        foreach (AssetsFileInstance fileInstance in bundleHelper.FileInstances)
        {
            fileInstance.file.Metadata.TargetPlatform = (uint)TargetPlatform.Ios;
            if (
                fileInstance.file.GetAssetsOfType(AssetClassID.Shader).Count != 0
                || fileInstance.file.GetAssetsOfType(AssetClassID.ComputeShader).Count != 0
            )
            {
                ConsoleApp.Log(
                    $"[WARNING] Shaders detected in asset {fileInstance.name} of bundle {bundleHelper.Path}"
                );
            }
        }

        using FileStream outputWrite = output.OpenWrite();
        bundleHelper.Write(outputWrite);
    }
}
