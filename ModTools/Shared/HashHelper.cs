using System.Security.Cryptography;

namespace ModTools.Shared;

internal static class HashHelper
{
    public static string GetHash(FileInfo assetBundle)
    {
        byte[] hash = SHA256.HashData(File.ReadAllBytes(assetBundle.FullName));
        return OtpNet.Base32Encoding.ToString(hash)[..52];
    }
}
