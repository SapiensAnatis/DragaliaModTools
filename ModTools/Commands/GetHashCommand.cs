using System.CommandLine;
using System.Security.Cryptography;
using ModTools.Shared;

namespace ModTools.Commands;

public class GetHashCommand : Command
{
    public GetHashCommand()
        : base("hash", "Get the hashed filename of an assetbundle.")
    {
        Argument<FileInfo> assetBundleArgument =
            new("assetbundle", description: "The asset bundle to get the hash of.");

        this.AddArgument(assetBundleArgument);

        this.SetHandler(DoHash, assetBundleArgument);
    }

    private static void DoHash(FileInfo assetBundle)
    {
        string hashName = HashHelper.GetHash(assetBundle);
        Console.WriteLine(hashName);
    }
}
