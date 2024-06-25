using ModTools.Shared;

namespace ModTools.Commands;

internal sealed class GetHashCommand
{
    /// <summary>
    /// Get the hash of an asset bundle.
    /// </summary>
    /// <param name="assetBundlePath">The path to the asset bundle to get the hash of.</param>
    [Command("hash")]
    public void Command([Argument] string assetBundlePath)
    {
        FileInfo inputFileInfo = new(assetBundlePath);
        string hashName = HashHelper.GetHash(inputFileInfo);
        ConsoleApp.Log(hashName);
    }
}
