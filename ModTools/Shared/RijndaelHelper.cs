using System.Collections.Immutable;
using System.Security.Cryptography;
#pragma warning disable SYSLIB0022 // The Rijndael and RijndaelManaged types are obsolete. Use Aes instead.

namespace ModTools.Shared;

public static class RijndaelHelper
{
    private static readonly byte[] Key = Convert.FromBase64String(
        "2JDKdLwjKMDLgxXGsI4AxBQ9t7d7of9Jp5gQkdBryoM="
    );

    private static readonly byte[] Iv = Convert.FromBase64String(
        "HzL3PqQVDY4H1QvMn5KghO+Is8NnJ+ydTYafQb+8HpI="
    );

    public static byte[] Encrypt(byte[] source)
    {
        using RijndaelManaged rijndael = new();
        rijndael.Key = Key;
        rijndael.IV = Iv;

        ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);
        using MemoryStream msEncrypt = new();
        using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);

        csEncrypt.Write(source);

        return msEncrypt.ToArray();
    }

    public static byte[] Decrypt(byte[] encrypted)
    {
        using RijndaelManaged rijndael = new();
        rijndael.Key = Key;
        rijndael.IV = Iv;

        ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
        using MemoryStream msDecrypt = new(encrypted);
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
        using MemoryStream output = new();

        csDecrypt.CopyTo(output);

        return output.ToArray();
    }
}
