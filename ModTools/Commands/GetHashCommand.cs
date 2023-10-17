using System.CommandLine;
using System.Security.Cryptography;

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
        SHA256 hasher = SHA256.Create();

        byte[] hash = hasher.ComputeHash(File.ReadAllBytes(assetBundle.FullName));
        string hashName = OtpNet.Base32Encoding.ToString(hash)[..52];

        Console.WriteLine(hashName);
    }
}
