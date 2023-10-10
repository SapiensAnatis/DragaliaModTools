using AssetsTools.NET;
using AssetsTools.NET.Extra;
using SerializableDictionaryPlugin;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Runtime.CompilerServices;

namespace DictionaryCli;

internal class Program
{
    private static async Task<int> Main(string[] args)
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

        AssetBundleHelperBinder binder = new(assetBundleArgument);
        OutputPathBinder outputBinder = new(assetBundleArgument, outputArgument, inPlaceOption);

        RootCommand rootCommand = new("Serializable dictionary helper.");

        rootCommand.AddArgument(assetBundleArgument);
        rootCommand.AddArgument(assetArgument);
        rootCommand.AddArgument(dictionaryArgument);
        rootCommand.AddArgument(outputArgument);
        rootCommand.AddOption(inPlaceOption);

        rootCommand.SetHandler(
            DoImport,
            assetBundleArgument,
            assetArgument,
            dictionaryArgument,
            outputBinder,
            binder
        );

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task DoImport(
        FileInfo assetBundlePath,
        string assetName,
        FileInfo dictionaryPath,
        FileInfo outputPath,
        AssetBundleHelper bundleHelper
    )
    {
        AssetTypeValueField field = bundleHelper.GetBaseField(assetName);

        SerializableDictionaryHelper.UpdateFromFile(dictionaryPath.FullName, field);

        bundleHelper.UpdateBaseField(assetName, field);

        using FileStream fs = outputPath.OpenWrite();
        bundleHelper.Write(fs);
    }

    private class AssetBundleHelperBinder(Argument<FileInfo> assetBundleArgument)
        : BinderBase<AssetBundleHelper>
    {
        protected override AssetBundleHelper GetBoundValue(BindingContext bindingContext)
        {
            FileInfo assetBundlePath = bindingContext.ParseResult.GetValueForArgument(
                assetBundleArgument
            );

            MemoryStream bundleMemoryStream = new(File.ReadAllBytes(assetBundlePath.FullName));

            AssetsManager manager = new();
            BundleFileInstance bundleFileInstance =
                new(bundleMemoryStream, assetBundlePath.FullName, unpackIfPacked: true);

            return new AssetBundleHelper(manager, bundleFileInstance);
        }
    }

    private class OutputPathBinder(
        Argument<FileInfo> assetBundleArgument,
        Argument<FileInfo?> outputPathArgument,
        Option<bool> inplaceOption
    ) : BinderBase<FileInfo>
    {
        protected override FileInfo GetBoundValue(BindingContext bindingContext)
        {
            bool inplace = bindingContext.ParseResult.GetValueForOption(inplaceOption);

            return inplace
                ? bindingContext.ParseResult.GetValueForArgument(assetBundleArgument)
                : bindingContext.ParseResult.GetValueForArgument(outputPathArgument)!;
        }
    }
}
