using System.CommandLine;
using AssetsTools.NET;
using ModTools.Shared;
using SerializableDictionaryPlugin.Shared;

namespace ModTools.Commands;

public class ImportDictionaryCommand : Command
{
    public ImportDictionaryCommand()
        : base("import", "Import a single serialized dictionary over an asset.")
    {
        Argument<FileInfo> assetBundleArgument =
            new(name: "assetbundle", description: "The path to the asset bundle to open.");

        Argument<string> assetArgument =
            new(name: "asset", description: "The asset within the bundle to edit.");

        Argument<FileInfo> dictionaryArgument =
            new(name: "dictionary", description: "The path to the dictionary JSON file to import.");

        Argument<FileInfo?> outputArgument =
            new(name: "output", () => null, description: "The desired output path.");

        Option<bool> inPlaceOption =
            new(name: "--inplace", description: "Specify to modify the file in-place.");

        outputArgument.AddValidator(result =>
        {
            if (
                !result.GetValueForOption(inPlaceOption)
                && result.GetValueForArgument(outputArgument) == null
            )
            {
                result.ErrorMessage =
                    "No output path specified. Use --inplace to modify the file in place.";
            }
        });

        AssetBundleHelperBinder assetBundleBinder = new(assetBundleArgument);
        OutputPathBinder outputPathBinder = new(assetBundleArgument, outputArgument, inPlaceOption);

        this.AddArgument(assetBundleArgument);
        this.AddArgument(assetArgument);
        this.AddArgument(dictionaryArgument);
        this.AddArgument(outputArgument);
        this.AddOption(inPlaceOption);

        this.SetHandler(
            DoImport,
            assetBundleArgument,
            assetArgument,
            dictionaryArgument,
            outputPathBinder,
            assetBundleBinder
        );
    }

    private static void DoImport(
        FileInfo assetBundlePath,
        string assetName,
        FileInfo dictionaryPath,
        FileInfo outputPath,
        AssetBundleHelper bundleHelper
    )
    {
        AssetTypeValueField field = bundleHelper.GetBaseField(assetName);

        Console.WriteLine("Importing file {0} over asset {1}", dictionaryPath, assetName);
        SerializableDictionaryHelper.UpdateFromFile(field, dictionaryPath.FullName);

        bundleHelper.UpdateBaseField(assetName, field);

        Console.WriteLine("Writing output to {0}", outputPath);
        using FileStream fs = outputPath.OpenWrite();
        bundleHelper.Write(fs);
    }
}
