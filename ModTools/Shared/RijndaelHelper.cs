using System.Diagnostics;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace ModTools.Shared;

internal static class RijndaelHelper
{
    private static readonly byte[] Key = Convert.FromBase64String(
        "2JDKdLwjKMDLgxXGsI4AxBQ9t7d7of9Jp5gQkdBryoM="
    );

    private static readonly byte[] Iv = Convert.FromBase64String(
        "HzL3PqQVDY4H1QvMn5KghO+Is8NnJ+ydTYafQb+8HpI="
    );

    public static byte[] Encrypt(byte[] source)
    {
        PaddedBufferedBlockCipher cipher = CreateCiper(forEncryption: true);

        byte[] encrypted = cipher.DoFinal(source);
        byte[] hash = SHA256.HashData(encrypted);

        byte[] final = new byte[encrypted.Length + hash.Length];
        encrypted.CopyTo(final, 0);
        hash.CopyTo(final, encrypted.Length);

        return final;
    }

    public static byte[] Decrypt(byte[] encrypted)
    {
        PaddedBufferedBlockCipher cipher = CreateCiper(forEncryption: false);
        return cipher.DoFinal(encrypted[..^32]); // Subtract SHA256 hash
    }

    private static PaddedBufferedBlockCipher CreateCiper(bool forEncryption)
    {
        RijndaelEngine engine = new(256);
        CbcBlockCipher blockCiper = new(engine);
        PaddedBufferedBlockCipher cipher = new(blockCiper, new Pkcs7Padding());
        KeyParameter keyParam = new(Key);
        ParametersWithIV keyParamWithIv = new(keyParam, Iv);
        cipher.Init(forEncryption, keyParamWithIv);

        return cipher;
    }
}
