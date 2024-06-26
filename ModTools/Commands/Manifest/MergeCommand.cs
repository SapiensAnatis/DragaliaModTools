﻿using System.Runtime.CompilerServices;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

internal sealed class MergeCommand
{
    /// <summary>
    /// Update the target manifest by adding any files only present in the source manifest.
    /// </summary>
    /// <param name="targetPath">--target|-t The path to the manifest that is the target of the merge.</param>
    /// <param name="sourcePath">--source|-s The path to the manifest that is the source of the merge.</param>
    /// <param name="outputManifestDir">--output-manifests|-m The path to a directory to output the merged manifest to.</param>
    /// <param name="outputBundleDir">--output-bundles|-b The path to a directory to output the new bundles to.</param>
    /// <param name="assetDirectories">--assets-path|-a Comma-separated list of directories to source the added asset bundles from.</param>
    /// <param name="conversion">--convert|-c Whether to convert assets to iOS during the merge process.</param>
    [Command("merge")]
    public void Command(
        string targetPath,
        string sourcePath,
        string outputManifestDir,
        string outputBundleDir,
        string[] assetDirectories,
        bool conversion
    )
    {
        using AssetBundleHelper targetHelper = AssetBundleHelper.FromPathEncrypted(targetPath);
        using AssetBundleHelper sourceHelper = AssetBundleHelper.FromPathEncrypted(sourcePath);

        DirectoryInfo outputBundleDirInfo = new(outputBundleDir);

        AssetTypeValueField targetBaseField = targetHelper.GetBaseField("manifest");
        AssetTypeValueField sourceBaseField = sourceHelper.GetBaseField("manifest");

        List<AssetTypeValueField> othersToAdd = GetDiff(
            targetBaseField,
            sourceBaseField,
            (manifest) => manifest["categories"]["Array"][1]["assets"]["Array"]
        );

        Directory.CreateDirectory(outputBundleDir);

        var assets = targetBaseField["categories"]["Array"][1]["assets"]["Array"].Children;

        var normalAsset = assets.First();

        foreach (AssetTypeValueField asset in othersToAdd)
        {
            string hash = asset["hash"].AsString;
            string assetPath = GetAssetPath(hash);
            // TODO multiple paths support again
            FileInfo bundleToAddPath = GetSourceFile(assetPath, assetDirectories);

            if (asset["assets"].IsDummy)
            {
                var newArray = ValueBuilder.DefaultValueFieldFromTemplate(
                    normalAsset["assets"].TemplateField
                );
                asset.Children.Add(newArray);

                PopulateAssetArray(newArray, bundleToAddPath);
            }

            if (conversion)
            {
                (FileInfo converted, string newHash) = PerformConversion(bundleToAddPath, asset);
                string newAssetPath = GetAssetPath(newHash);
                CopyToOutput(converted, outputBundleDirInfo, newAssetPath);
            }
            else
            {
                CopyToOutput(bundleToAddPath, outputBundleDirInfo, assetPath);
            }
        }

        targetBaseField["categories"]["Array"][1]["assets"]["Array"].Children.AddRange(othersToAdd);

        List<AssetTypeValueField> rawsToAdd = GetDiff(
            targetBaseField,
            sourceBaseField,
            (manifest) => manifest["rawAssets"]["Array"]
        );

        targetBaseField["rawAssets"]["Array"].Children.AddRange(rawsToAdd);

        targetHelper.UpdateBaseField("manifest", targetBaseField);

        string outputPath = Path.Join(
            Path.GetDirectoryName(outputBundleDir),
            Path.GetFileName(sourcePath)
        );

        MemoryStream ms = new();
        targetHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        ConsoleApp.Log($"Writing output to {outputPath}");
        File.WriteAllBytes(outputPath, encrypted);
    }

    private static List<AssetTypeValueField> GetDiff(
        AssetTypeValueField target,
        AssetTypeValueField source,
        Func<AssetTypeValueField, AssetTypeValueField> path,
        [CallerArgumentExpression(nameof(path))] string? pathName = null
    )
    {
        AssetTypeValueField targetAssets = path.Invoke(target);
        AssetTypeValueField sourceAssets = path.Invoke(source);

        List<AssetTypeValueField> assetsToAdd = sourceAssets
            .Except(targetAssets, ManifestAssetComparer.Instance)
            .ToList();

        ConsoleApp.Log($"Found {assetsToAdd.Count} assets to add to {pathName}");

        return assetsToAdd;
    }

    private static string GetAssetPath(string hash)
    {
        return Path.Join(hash[..2], hash);
    }

    private static (FileInfo convertedPath, string newHash) PerformConversion(
        FileInfo sourcePath,
        AssetTypeValueField asset
    )
    {
        string tempFileName = Path.GetTempFileName();
        FileInfo outputFileInfo = new(tempFileName);

        BundleConversionHelper.ConvertToIos(sourcePath, outputFileInfo);
        string newHash = HashHelper.GetHash(outputFileInfo);
        asset["hash"].AsString = newHash;

        return (new FileInfo(tempFileName), newHash);
    }

    private static void CopyToOutput(
        FileInfo sourcePath,
        DirectoryInfo outputDirectory,
        string assetPath
    )
    {
        string newPath = Path.Join(outputDirectory.FullName, assetPath);
        string newDirectory =
            Path.GetDirectoryName(newPath)
            ?? throw new InvalidOperationException("Failed to get directory name");

        Directory.CreateDirectory(newDirectory);

        File.Copy(sourcePath.FullName, newPath, overwrite: true);
    }

    private static FileInfo GetSourceFile(string assetPath, IEnumerable<string> directories)
    {
        foreach (string directory in directories)
        {
            string path = Path.Join(directory, assetPath);
            if (File.Exists(path))
            {
                return new FileInfo(path);
            }
        }

        throw new IOException($"Failed to find asset {assetPath} in any configured directory");
    }

    private static void PopulateAssetArray(AssetTypeValueField newAssetVector, FileInfo bundlePath)
    {
        var newArray = newAssetVector["Array"];
        byte[] bundleData = File.ReadAllBytes(bundlePath.FullName);

        using AssetBundleHelper helper = AssetBundleHelper.FromData(
            bundleData,
            bundlePath.FullName
        );

        var containers = helper
            .GetAllBaseFields(0)
            .Select(GetContainer)
            .Where(x => x != null)
            .Select(x =>
            {
                var newValue = ValueBuilder.DefaultValueFieldFromArrayTemplate(newArray);
                newValue.Value.AsString = x;
                return newValue;
            });

        newArray.Children.AddRange(containers);
    }

    private static string? GetContainer(AssetTypeValueField assetField)
    {
        if (assetField["m_Container"] is not { IsDummy: false } container)
            return null;

        string containerName = container[0][0][0].AsString;
        containerName = containerName.Replace(
            "assets/_gluonresources/",
            "",
            StringComparison.Ordinal
        );
        containerName = containerName.Replace("resources/", "", StringComparison.Ordinal);
        return containerName;
    }
}

file sealed class ManifestAssetComparer : IEqualityComparer<AssetTypeValueField>
{
    public static ManifestAssetComparer Instance { get; } = new();

    public bool Equals(AssetTypeValueField? x, AssetTypeValueField? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        AssetTypeValueField xName = x["name"];
        if (xName.IsDummy)
            throw new ArgumentException("Not a manifest asset", nameof(x));

        AssetTypeValueField yName = y["name"];
        if (yName.IsDummy)
            throw new ArgumentException("Not a manifest asset", nameof(y));

        return xName.AsString == yName.AsString;
    }

    public int GetHashCode(AssetTypeValueField obj)
    {
        AssetTypeValueField name = obj["name"];

        if (name.IsDummy)
            throw new ArgumentException("Not a manifest asset", nameof(obj));

        return name.AsString.GetHashCode(StringComparison.Ordinal);
    }
}
