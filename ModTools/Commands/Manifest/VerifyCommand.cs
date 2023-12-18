using System.CommandLine;
using System.Security.Cryptography;
using System.Text;

namespace ModTools.Commands.Manifest;

public class VerifyCommand : Command
{
    public VerifyCommand()
        : base("verify", "Verify the integrity of an encrypted manifest.")
    {
        Argument<FileInfo> manifestArgument = new("manifest", "Path to the manifest to verify.");

        this.AddArgument(manifestArgument);

        this.SetHandler(DoVerification, manifestArgument);
    }

    private static void DoVerification(FileInfo manifestPath)
    {
        ReadOnlySpan<byte> fileBytes = File.ReadAllBytes(manifestPath.FullName);

        ReadOnlySpan<byte> finalBytes = fileBytes[^32..];
        ReadOnlySpan<byte> hashBytes = SHA256.HashData(fileBytes[..^32]);

        string finalBytesString = Convert.ToBase64String(finalBytes);
        string hashString = Convert.ToBase64String(hashBytes);

        Console.WriteLine($"Final 32 bytes: {finalBytesString}, hash: {hashString}");
    }
}
