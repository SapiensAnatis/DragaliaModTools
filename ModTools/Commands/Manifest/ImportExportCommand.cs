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

    private static Manifest GetManifestFields(AssetTypeValueField manifestField)
    {
        AssetTypeValueField? masterCategory = manifestField["categories"]
            ["Array"]
            .FirstOrDefault(x => x["name"].AsString == "Master");

        if (masterCategory?["assets"]["Array"] is not { IsDummy: false } masterArray)
        {
            throw new InvalidOperationException(
                "Malformed manifest - failed to find 'Master' asset array"
            );
        }

        AssetTypeValueField? othersCategory = manifestField["categories"]
            ["Array"]
            .FirstOrDefault(x => x["name"].AsString == "Others");

        if (othersCategory?["assets"]["Array"] is not { IsDummy: false } othersArray)
        {
            throw new InvalidOperationException(
                "Malformed manifest - failed to find 'Others' asset array"
            );
        }

        AssetTypeValueField? rawAssetArray = manifestField["rawAssets"]["Array"];

        if (rawAssetArray is not { IsDummy: false })
        {
            throw new InvalidOperationException(
                "Malformed manifest - failed to find rawAssets array"
            );
        }

        return new()
        {
            MasterArray = masterArray,
            OthersArray = othersArray,
            RawAssetsArray = rawAssetArray
        };
    }

    private sealed class Manifest
    {
        public required AssetTypeValueField MasterArray { get; init; }

        public required AssetTypeValueField OthersArray { get; init; }

        public required AssetTypeValueField RawAssetsArray { get; init; }

        public AssetTypeTemplateField GetExportTemplate()
        {
            return new AssetTypeTemplateField
            {
                HasValue = false,
                IsArray = false,
                ValueType = AssetValueType.None,
                Children =
                [
                    new()
                    {
                        Name = "master",
                        IsArray = true,
                        ValueType = this.MasterArray.TemplateField.ValueType,
                        Children = this.MasterArray.TemplateField.Children
                    },
                    new()
                    {
                        Name = "others",
                        IsArray = true,
                        ValueType = this.OthersArray.TemplateField.ValueType,
                        Children = this.OthersArray.TemplateField.Children
                    },
                    new()
                    {
                        Name = "rawAssets",
                        IsArray = true,
                        ValueType = this.RawAssetsArray.TemplateField.ValueType,
                        Children = this.RawAssetsArray.TemplateField.Children
                    }
                ],
            };
        }
    }
}
