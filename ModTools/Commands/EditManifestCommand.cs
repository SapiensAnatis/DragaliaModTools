using System.CommandLine;
using ModTools.Shared;

namespace ModTools.Commands;

public class EditManifestCommand : Command
{
    public EditManifestCommand()
        : base("manifest", "Update a file's hash and size in a manifest.")
    {
        Argument<FileInfo> manifestArgument =
            new("manifest", description: "Path to the encrypted manifest to update.");

        Argument<string> assetNameArgument =
            new("assetname", description: "Name of the asset in the manifest to update.");

        Argument<FileInfo> assetBundleArgument =
            new("assetbundle", description: "The asset bundle to update with.");

        Argument<FileInfo> outputArgument =
            new("output", description: "The path to save the result to.");

        Option<bool> encryptOption =
            new("no-reencrypt", description: "Skip re-encrypting the manifest.");

        EncryptedAssetBundleHelperBinder manifestBinder = new(manifestArgument);

        this.AddArgument(manifestArgument);
        this.AddArgument(assetNameArgument);
        this.AddArgument(assetBundleArgument);
        this.AddArgument(outputArgument);
        this.AddOption(encryptOption);

        this.SetHandler(
            DoEdit,
            manifestBinder,
            assetNameArgument,
            assetBundleArgument,
            outputArgument,
            encryptOption
        );
    }

    private static void DoEdit(
        AssetBundleHelper manifestHelper,
        string assetName,
        FileInfo assetBundlePath,
        FileInfo outputPath,
        bool reEncrypt
    ) { }
}
