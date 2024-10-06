using AssetsTools.NET;
using AssetsTools.NET.Extra;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

internal sealed class AddBundleCommand
{
    /// <summary>
    /// Add a bundle into the 'others' asset array of an encrypted manifest asset bundle.
    /// </summary>
    /// <param name="manifestPath">The path to the encrypted manifest.</param>
    /// <param name="bundleDirectory">--bundles|-b, The path to a folder containing asset bundles.</param>
    /// <param name="outputPath">--output|-o, The path to write the updated encrypted manifest to.</param>
    [Command("add-bundles")]
    public async Task Command(
        [Argument] string manifestPath,
        string bundleDirectory,
        string outputPath
    )
    {
        using AssetBundleHelper manifestHelper = AssetBundleHelper.FromPathEncrypted(manifestPath);

        AssetTypeValueField manifestField = manifestHelper.GetBaseField("manifest");

        AssetTypeValueField? othersCategory = manifestField["categories.Array"]
            .FirstOrDefault(x => x["name"].AsString == "Others");

        if (othersCategory?["assets.Array"] is not { IsDummy: false } othersArray)
        {
            throw new InvalidOperationException(
                "Malformed manifest - failed to find 'Others' asset array"
            );
        }

        foreach (
            var bundleFilePath in Directory.EnumerateFiles(
                bundleDirectory,
                "*",
                SearchOption.AllDirectories
            )
        )
        {
            AssetTypeValueField newEntry = BuildNewOthersArrayEntry(othersArray, bundleFilePath);
            othersArray.Children.Add(newEntry);
        }

        manifestHelper.UpdateBaseField("manifest", manifestField);

        MemoryStream ms = new();
        manifestHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        ConsoleApp.Log($"Writing output to {outputPath}");
        await File.WriteAllBytesAsync(outputPath, encrypted);
    }

    private static AssetTypeValueField BuildNewOthersArrayEntry(
        AssetTypeValueField othersArray,
        string filepath
    )
    {
        FileInfo bundleFileInfo = new FileInfo(filepath);
        using AssetBundleHelper bundleHelper = AssetBundleHelper.FromPath(filepath);

        var assetBundleInfo = bundleHelper.GetBaseField(1);

        string name = assetBundleInfo["m_AssetBundleName"]
            .AsString.Replace(".a", "", StringComparison.Ordinal);
        string hash = HashHelper.GetHash(new FileInfo(filepath));
        long size = bundleFileInfo.Length;
        int group = 2; // I have no idea what this does or how it's determined

        ConsoleApp.Log($"Adding bundle {filepath} to manifest - name: {name}, hash: {hash}");

        AssetTypeValueField newArrayElement = ValueBuilder.DefaultValueFieldFromArrayTemplate(
            othersArray.TemplateField
        );

        List<AssetTypeValueField> dependencies = [];
        foreach (AssetTypeValueField? dependency in assetBundleInfo["m_Dependencies.Array"])
        {
            AssetTypeValueField entry = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                newArrayElement["assets.Array"]
            );

            entry.Value.AsString = dependency.Value.AsString.Replace(
                ".a",
                "",
                StringComparison.Ordinal
            );

            dependencies.Add(entry);
        }

        List<AssetTypeValueField> assets = [];
        foreach (
            AssetTypeValueField? containerChild in assetBundleInfo["m_Container.Array"].Children
        )
        {
            AssetTypeValueField entry = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                newArrayElement["dependencies.Array"]
            );

            entry.Value.AsString = containerChild[0]
                .AsString.Replace(
                    "assets/_gluonresources/resources/",
                    "",
                    StringComparison.Ordinal
                );

            assets.Add(entry);
        }

        newArrayElement["name"].AsString = name;
        newArrayElement["hash"].AsString = hash;
        newArrayElement["dependencies.Array"].Children = dependencies;
        newArrayElement["size"].AsLong = size;
        newArrayElement["group"].AsInt = group;
        newArrayElement["assets.Array"].Children = assets;

        return newArrayElement;
    }
}
