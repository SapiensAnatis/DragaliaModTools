using System.Security.Cryptography;

namespace ModTools.Commands.Manifest;

internal sealed class VerifyCommand
{
    /// <summary>
    /// Verify the integrity of an encrypted manifest.
    /// </summary>
    /// <param name="manifestPath">Path to the manifest to verify.</param>
    [Command("verify")]
    public void Command([Argument] string manifestPath)
    {
        ReadOnlySpan<byte> fileBytes = File.ReadAllBytes(manifestPath);

        ReadOnlySpan<byte> finalBytes = fileBytes[^32..];
        ReadOnlySpan<byte> hashBytes = SHA256.HashData(fileBytes[..^32]);

        string finalBytesString = Convert.ToBase64String(finalBytes);
        string hashString = Convert.ToBase64String(hashBytes);

        ConsoleApp.Log($"Final 32 bytes: {finalBytesString}, hash: {hashString}");
    }
}
