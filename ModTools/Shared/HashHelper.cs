using System.Security.Cryptography;

namespace ModTools.Shared;

internal static class HashHelper
{
    private static ReadOnlySpan<char> Base32Alphabet => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string GetHash(FileInfo assetBundle)
    {
        using FileStream fileStream = File.OpenRead(assetBundle.FullName);
        return GetHash(fileStream);
    }

    public static string GetHash(AssetBundleHelper assetBundle)
    {
        return GetHash(assetBundle.DataStream);
    }

    private static string GetHash(Stream assetBundleStream)
    {
        Span<byte> hash = stackalloc byte[32];
        Span<char> dest = stackalloc char[52];

        SHA256.HashData(assetBundleStream, hash);

        Base32Encode(hash, dest);

        return new string(dest);
    }

    private static void Base32Encode(ReadOnlySpan<byte> input, Span<char> output)
    {
        int bitIndex = 0;
        for (int i = 0; i < 52; i++)
        {
            int byteOffset = bitIndex / 8;
            int bitOffset = bitIndex % 8;

            int num = input[byteOffset] << 8;
            if (byteOffset + 1 < input.Length)
            {
                num |= input[byteOffset + 1];
            }

            int chunk = (num >> (16 - 5 - bitOffset)) & 0b11111;
            output[i] = Base32Alphabet[chunk];
            bitIndex += 5;
        }

        // We don't need to worry about padding since the game explicitly truncates all
        // hashes to 52 characters instead of the 56 with padding
    }
}
