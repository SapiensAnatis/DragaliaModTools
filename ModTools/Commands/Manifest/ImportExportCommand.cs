using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using ModTools.Shared;
using SerializableDictionaryPlugin.Shared;

namespace ModTools.Commands.Manifest;

internal sealed class ImportExportCommand
{
    /// <summary>
    /// Export the manifest into an editable JSON format.
    /// </summary>
    /// <param name="manifestPath">The path to the encrypted manifest asset bundle.</param>
    /// <param name="outputPath">--output|-o, The path to write the exported JSON file to.</param>
    [Command("export")]
    public async Task ExportCommand([Argument] string manifestPath, string outputPath)
    {
        using AssetBundleHelper manifestHelper = AssetBundleHelper.FromPathEncrypted(manifestPath);
        AssetTypeValueField manifestField = manifestHelper.GetBaseField("manifest");

        await using FileStream outputFs = File.Open(outputPath, FileMode.Create, FileAccess.Write);

        AssetSerializer.Serialize(outputFs, manifestField);

        ConsoleApp.Log($"Successfully exported manifest to {outputPath}");
    }

    /// <summary>
    /// Import a previously exported JSON file over an encrypted manifest asset bundle.
    /// </summary>
    /// <param name="jsonPath">The path to the previously exported manifest.</param>
    /// <param name="manifestPath">--manifest|-m, The path to the encrypted manifest to import into.</param>
    /// <param name="outputPath">--output|-o, The path to write the updated encrypted manifest to</param>
    [Command("import")]
    public async Task ImportCommand(
        [Argument] string jsonPath,
        string manifestPath,
        string outputPath
    )
    {
        using AssetBundleHelper manifestHelper = AssetBundleHelper.FromPathEncrypted(manifestPath);
        AssetTypeValueField manifestField = manifestHelper.GetBaseField("manifest");

        // TODO: Allow reading from disk and set SharedOptionContext
        using MemoryStream inputStream = new MemoryStream(await File.ReadAllBytesAsync(jsonPath));

        AssetTypeValueField deserialized = AssetSerializer.Deserialize(
            inputStream,
            manifestField.TemplateField
        );

        manifestHelper.UpdateBaseField("manifest", deserialized);

        MemoryStream ms = new();
        manifestHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        ConsoleApp.Log($"Writing output to {outputPath}");
        await File.WriteAllBytesAsync(outputPath, encrypted);
    }
}
