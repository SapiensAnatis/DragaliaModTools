using AssetsTools.NET.Extra;
using ModTools.Shared;
using Org.BouncyCastle.Asn1.X509;
using System.CommandLine;
using System.Runtime.CompilerServices;

namespace ModTools.Commands;

public class ConvertBundleCommand : Command
{
    public ConvertBundleCommand()
        : base("convert", "Converts an Android asset bundle to iOS.")
    {
        Argument<FileInfo> inputPathArgument = new("bundle", "Path to target asset bundle.");
        Argument<FileInfo> outputPathArgument = new("output", "Desired output path.");

        AssetBundleHelperBinder helperBinder = new(inputPathArgument);

        this.AddArgument(inputPathArgument);
        this.AddArgument(outputPathArgument);

        this.SetHandler(DoConversion, inputPathArgument, outputPathArgument, helperBinder);
    }

    private static void DoConversion(
        FileInfo inputPath,
        FileInfo outputPath,
        AssetBundleHelper bundleHelper
    )
    {
        BundleConversionHelper.ConvertToIos(bundleHelper, outputPath);
    }
}
