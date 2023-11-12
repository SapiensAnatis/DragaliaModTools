using System.CommandLine;
using System.Runtime.CompilerServices;
using AssetsTools.NET;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

public class EditCommand : Command
{
    public EditCommand()
        : base("edit-master", "Update the master asset's hash and size in a manifest.")
    {
        Argument<FileInfo> manifestArgument =
            new("manifest", description: "Path to the encrypted manifest to update.");

        Argument<FileInfo> assetBundleArgument =
            new("assetbundle", description: "The asset bundle to update with.");

        Argument<FileInfo> outputArgument =
            new("output", description: "The path to save the result to.");

        Option<bool> debugOption = new("debug", description: "Leave intermediate-stage files.");
        {
            IsHidden = true;
        }

        EncryptedAssetBundleHelperBinder manifestBinder = new(manifestArgument);

        AddArgument(manifestArgument);
        AddArgument(assetBundleArgument);
        AddArgument(outputArgument);
        AddOption(debugOption);

        this.SetHandler(DoEdit, manifestBinder, assetBundleArgument, outputArgument, debugOption);
    }

    private static void DoEdit(
        AssetBundleHelper manifestHelper,
        FileInfo assetBundlePath,
        FileInfo outputPath,
        bool debug
    )
    {
        AssetTypeValueField manifestField = manifestHelper.GetBaseField("manifest");

        AssetTypeValueField? master = manifestField["categories"]["Array"][0]["assets"]["Array"][0];

        Console.WriteLine(
            "Updating master hash and size from [{0}, {1}] to [{2}, {3}]",
            master["hash"].AsString,
            master["size"].AsInt,
            assetBundlePath.Name,
            assetBundlePath.Length
        );

        master["hash"].AsString = assetBundlePath.Name;
        master["size"].AsInt = (int)assetBundlePath.Length;

        manifestHelper.UpdateBaseField("manifest", manifestField);

        Console.WriteLine("Encrypting output");

        MemoryStream ms = new();
        manifestHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        Console.WriteLine("Writing output to {0}", outputPath);
        File.WriteAllBytes(outputPath.FullName, encrypted);
    }
}
