using System.CommandLine;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

public class DecryptCommand : Command
{
    public DecryptCommand()
        : base("decrypt", "Decrypt a manifest")
    {
        Argument<FileInfo> manifestArgument = new("manifest", "Path to the manifest to decrypt");

        EncryptedAssetBundleHelperBinder binder = new(manifestArgument);

        this.AddArgument(manifestArgument);
        this.SetHandler(DoDecryption, manifestArgument, binder);
    }

    private static void DoDecryption(FileInfo path, AssetBundleHelper manifest)
    {
        using FileStream fs = File.OpenWrite(path.FullName + ".decrypted");
        manifest.Write(fs);
    }
}
