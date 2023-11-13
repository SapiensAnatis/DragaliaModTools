using System.Security.Cryptography;

namespace ModTools.Shared;

public static class HashHelper
{
    public static string GetHash(FileInfo assetBundle)
    {
        SHA256 hasher = SHA256.Create();

        byte[] hash = hasher.ComputeHash(File.ReadAllBytes(assetBundle.FullName));
        return OtpNet.Base32Encoding.ToString(hash)[..52];
    }
}
