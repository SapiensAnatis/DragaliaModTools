using System.CommandLine;
using AssetsTools.NET;
using Microsoft.Extensions.Logging;
using ModTools.Shared;
using SerializableDictionaryPlugin;

namespace ModTools.Commands;

public class ImportMultipleDictionaryCommand : Command
{
    public ImportMultipleDictionaryCommand()
        : base("import-multiple", "Import a directory of serializable dictionary files into an asset bundle.")
    {
        Argument<FileInfo> assetBundleArgument =
            new(name: "assetbundle", description: "The path to the asset bundle to open.");

        Argument<DirectoryInfo> directoryArgument =
            new(name: "directory", description: "The directory containing files to import. " +
                                                "All .json files will be imported over the asset matching their name, sans extension.");

        Argument<FileInfo?> outputArgument =
            new(name: "output", () => null, description: "The desired asset bundle output path.");

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
        LoggerBinder loggerBinder = new();
        
        this.AddArgument(assetBundleArgument);
        this.AddArgument(directoryArgument);
        this.AddArgument(outputArgument);
        this.AddOption(inPlaceOption);
        
        this.SetHandler(
            DoImport,
            assetBundleArgument,
            directoryArgument,
            outputPathBinder,
            assetBundleBinder,
            loggerBinder  
        );
    }

    private static void DoImport(
        FileInfo assetBundlePath,
        DirectoryInfo directoryInfo,
        FileInfo outputPath,
        AssetBundleHelper bundleHelper,
        ILogger logger
    )
    {
        foreach (FileInfo file in directoryInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly))
        {
            string assetName = Path.GetFileNameWithoutExtension(file.Name);
            logger.LogInformation("Importing file {file} over asset {asset}", file.Name, assetName);
            
            AssetTypeValueField field = bundleHelper.GetBaseField(assetName);

            SerializableDictionaryHelper.UpdateFromFile(file.FullName, field);

            bundleHelper.UpdateBaseField(assetName, field);
        }
        
        logger.LogInformation("Writing output to {output}", outputPath);
        using FileStream fs = outputPath.OpenWrite();
        bundleHelper.Write(fs);
    }
}
