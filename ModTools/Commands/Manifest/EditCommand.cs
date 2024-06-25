using AssetsTools.NET;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

internal sealed class EditCommand
{
    /// <summary>
    /// Update the master asset's hash and size in a manifest.
    /// </summary>
    /// <param name="manifestPath">The path to the manifest to update.</param>
    /// <param name="masterPath">--master|-m The path to the master asset to update with.</param>
    /// <param name="outputPath">--output|-o The path to write the updated manifest to.</param>
    [Command("edit-master")]
    public void Command([Argument] string manifestPath, string masterPath, string outputPath)
    {
        using AssetBundleHelper manifestHelper = AssetBundleHelper.FromPathEncrypted(manifestPath);
        FileInfo masterFileInfo = new(masterPath);

        AssetTypeValueField manifestField = manifestHelper.GetBaseField("manifest");

        AssetTypeValueField? master = manifestField["categories"]["Array"][0]["assets"]["Array"][0];

        ConsoleApp.Log(
            $"Updating master hash and size from [{master["hash"].AsString}, {master["size"].AsInt}] to [{masterFileInfo.Name}, {masterFileInfo.Length}]"
        );

        master["hash"].AsString = masterFileInfo.Name;
        master["size"].AsInt = (int)masterFileInfo.Length;

        manifestHelper.UpdateBaseField("manifest", manifestField);

        ConsoleApp.Log("Encrypting output");

        MemoryStream ms = new();
        manifestHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        ConsoleApp.Log($"Writing output to {outputPath}");
        File.WriteAllBytes(outputPath, encrypted);
    }
}
