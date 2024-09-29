using ModTools.Shared;

namespace ModTools.Commands.Manifest;

internal sealed class DecryptCommand
{
    /// <summary>
    /// Decrypt a manifest.
    /// </summary>
    /// <param name="path">The path to the manifest to decrypt.</param>
    [Command("decrypt")]
    public void Command([Argument] string path)
    {
        using AssetBundleHelper manifest = AssetBundleHelper.FromPathEncrypted(path);

        using FileStream fs = File.OpenWrite(path + ".decrypted");
        manifest.Write(fs);
    }
}
