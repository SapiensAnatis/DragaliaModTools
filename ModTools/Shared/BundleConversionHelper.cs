using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ModTools.Shared;

public static class BundleConversionHelper
{
    public static void ConvertToIos(FileInfo input, FileInfo output)
    {
        AssetBundleHelper bundleHelper = AssetBundleHelper.FromData(
            File.ReadAllBytes(input.FullName),
            input.FullName
        );

        ConvertToIos(bundleHelper, output);
    }

    public static void ConvertToIos(AssetBundleHelper bundleHelper, FileInfo output)
    {
        bundleHelper.FileInstance.file.Metadata.TargetPlatform = (uint)TargetPlatform.Ios;
        if (
            bundleHelper.FileInstance.file.GetAssetsOfType(AssetClassID.Shader).Any()
            || bundleHelper.FileInstance.file.GetAssetsOfType(AssetClassID.ComputeShader).Any()
        )
        {
            throw new NotSupportedException(
                $"Cannot convert shader in file {bundleHelper.FileInstance.path}"
            );
        }

        using FileStream outputWrite = output.OpenWrite();
        bundleHelper.Write(outputWrite);
    }
}
