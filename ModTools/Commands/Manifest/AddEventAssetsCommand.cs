using System.Diagnostics;
using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using ModTools.Shared;

namespace ModTools.Commands.Manifest;

internal sealed partial class AddEventAssetsCommand
{
    /// <summary>
    /// Update the target manifest by adding files required for the event given by eventId.
    /// </summary>
    /// <param name="target">The path to the manifest that is the target of the merge.</param>
    /// <param name="targetLocale">The locale that the target manifest represents.</param>
    /// <param name="eventId">--event|-e, The path to the manifest that is the source of the merge.</param>
    /// <param name="outputManifestDir">The path to a directory to output the merged manifest to.</param>
    /// <param name="outputBundleDir">The path to a directory to output the new bundles to.</param>
    /// <param name="assetsDir">Directory to source the added asset bundles from.</param>
    /// <param name="parsedManifestsDir">Path to a clone of https://github.com/DragaliaLostRevival/DragaliaManifests</param>
    /// <param name="conversion">--convert|-c, Whether to convert assets to iOS during the merge process.</param>
    [Command("add-event-assets")]
    public async Task Command(
        string target,
        string targetLocale,
        int eventId,
        string parsedManifestsDir,
        string outputManifestDir,
        string outputBundleDir,
        string assetsDir,
        bool conversion
    )
    {
        using var targetHelper = AssetBundleHelper.FromPathEncrypted(target);

        ManifestAssetCollection uniqueEventAssets = await GetUniqueEventAssets(
            eventId,
            parsedManifestsDir,
            targetLocale
        );

        AssetTypeValueField targetBaseField = targetHelper.GetBaseField("manifest");

        foreach (ManifestAsset asset in uniqueEventAssets.MainAssets)
        {
            string bundlePath = Path.Combine(assetsDir, GetAssetPath(asset.Hash));

            Console.WriteLine($"Adding asset: ${asset.Name}");

            if (conversion)
            {
                using var helper = AssetBundleHelper.FromPath(bundlePath);

                string oldHash = HashHelper.GetHash(helper);
                BundleConversionHelper.ConvertToIos(helper);
                string newHash = HashHelper.GetHash(helper);
                Debug.Assert(oldHash != newHash);

                string outputPath = Path.Combine(outputBundleDir, GetAssetPath(newHash));
                CreateDirectoryAndCopy(bundlePath, outputPath);

                await using FileStream fs = File.OpenWrite(outputPath);

                helper.Write(fs);

                AddAssetToManifest(targetBaseField, asset with { Hash = newHash }, bundlePath);
            }
            else
            {
                string outputPath = Path.Combine(outputBundleDir, GetAssetPath(asset.Hash));
                CreateDirectoryAndCopy(bundlePath, outputPath);

                AddAssetToManifest(targetBaseField, asset, bundlePath);
            }
        }

        foreach (ManifestAsset asset in uniqueEventAssets.RawAssets)
        {
            string bundlePath = Path.Combine(assetsDir, GetAssetPath(asset.Hash));
            string outputPath = Path.Combine(outputBundleDir, GetAssetPath(asset.Hash));
            CreateDirectoryAndCopy(bundlePath, outputPath);

            AddRawAssetToManifest(targetBaseField, asset, bundlePath);
        }

        targetHelper.UpdateBaseField("manifest", targetBaseField);

        string resultOutputPath = Path.Combine(outputManifestDir, Path.GetFileName(target));

        MemoryStream ms = new();
        targetHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        ConsoleApp.Log($"Writing output to {resultOutputPath}");
        await File.WriteAllBytesAsync(resultOutputPath, encrypted);
    }

    private static async Task<ManifestAssetCollection> GetAssets(
        string parsedManifestsDir,
        string manifestName,
        string locale
    )
    {
        string filename =
            locale == "ja_jp" ? "assetbundle.manifest.json" : $"assetbundle.{locale}.manifest.json";

        string path = Path.Combine(parsedManifestsDir, "Android", manifestName, filename);

        await using FileStream fs = File.OpenRead(path);

        JsonDocument manifest = await JsonDocument.ParseAsync(fs);

        List<ManifestAsset>? others = manifest
            .RootElement.GetProperty("categories")[1]
            .GetProperty("assets")
            .Deserialize<List<ManifestAsset>>(ModToolsSerializerContext.Default.ListManifestAsset);
        List<ManifestAsset>? raws = manifest
            .RootElement.GetProperty("rawAssets")
            .Deserialize<List<ManifestAsset>>(ModToolsSerializerContext.Default.ListManifestAsset);

        if (others is null || raws is null)
        {
            throw new UnreachableException("Manifest parsing fail");
        }

        return new(
            new HashSet<ManifestAsset>(others, ManifestAssetNameComparer.Instance),
            new HashSet<ManifestAsset>(raws, ManifestAssetNameComparer.Instance)
        );
    }

    private static async Task<ManifestAssetCollection> GetUniqueEventAssets(
        int eventId,
        string parsedManifestsDir,
        string locale
    )
    {
        if (!LatestEventRunStartDates.TryGetValue(eventId, out EventPeriod period))
        {
            throw new ArgumentException("Invalid or unrecognised event ID", nameof(eventId));
        }

        // They don't always seem to clean up the assets after the event is over, so diffing the manifest before
        // the event started and the one delivered with the event seems like the most reliable strategy

        string preEventManifest = ManifestDates
            .Where(x => x.Date < period.StartDate)
            .MinBy(x => Math.Abs(x.Date.DayNumber - period.StartDate.DayNumber))
            .FolderName;

        string eventManifest = ManifestDates
            .Where(x => x.Date >= period.StartDate)
            .MinBy(x => Math.Abs(x.Date.DayNumber - period.StartDate.DayNumber))
            .FolderName;

        Console.WriteLine($"Event {eventId} ran between {preEventManifest} | {eventManifest}");

        var preEventAssets = await GetAssets(parsedManifestsDir, preEventManifest, locale);
        var eventAssets = await GetAssets(parsedManifestsDir, eventManifest, locale);
        var currentAssets = await GetAssets(
            parsedManifestsDir,
            "20221002_y2XM6giU6zz56wCm",
            locale
        ); // Assumes you are adding assets to the newest manifest

        var uniqueEventAssets = new HashSet<ManifestAsset>(
            eventAssets.MainAssets,
            ManifestAssetNameComparer.Instance
        );

        var uniqueRawAssets = new HashSet<ManifestAsset>(
            eventAssets.RawAssets,
            ManifestAssetNameComparer.Instance
        );

        uniqueEventAssets.ExceptWith(preEventAssets.MainAssets);
        uniqueEventAssets.ExceptWith(currentAssets.MainAssets); // Don't copy assets for new characters/dragons/etc that stayed after the event

        uniqueRawAssets.ExceptWith(preEventAssets.RawAssets);
        uniqueRawAssets.ExceptWith(currentAssets.RawAssets);

        return new ManifestAssetCollection(uniqueEventAssets, uniqueRawAssets);
    }

    private static void AddAssetToManifest(
        AssetTypeValueField targetField,
        ManifestAsset assetToAdd,
        string assetPath
    )
    {
        ManifestAsset updatedAsset = assetToAdd;

        if (assetToAdd.Assets is null)
        {
            updatedAsset = updatedAsset with { Assets = DeriveAssetList(assetPath) };
        }

        AssetTypeValueField? array = targetField["categories"]["Array"][1]["assets"]["Array"];

        AssetTypeValueField childToAdd = CreateFieldFromAssetModel(
            array.TemplateField,
            updatedAsset,
            assetPath
        );
        array.Children.Add(childToAdd);
    }

    private static void AddRawAssetToManifest(
        AssetTypeValueField targetField,
        ManifestAsset assetToAdd,
        string assetPath
    )
    {
        AssetTypeValueField? array = targetField["rawAssets"]["Array"];

        AssetTypeValueField childToAdd = CreateFieldFromAssetModel(
            array.TemplateField,
            assetToAdd,
            assetPath
        );
        array.Children.Add(childToAdd);
    }

    private static string GetAssetPath(string hash)
    {
        return Path.Join(hash[..2], hash);
    }

    private static AssetTypeValueField CreateFieldFromAssetModel(
        AssetTypeTemplateField arrayTemplate,
        ManifestAsset manifestAsset,
        string assetPath
    )
    {
        AssetTypeValueField? field = ValueBuilder.DefaultValueFieldFromArrayTemplate(arrayTemplate);

        field["name"].AsString = manifestAsset.Name;
        field["hash"].AsString = manifestAsset.Hash;

        if (manifestAsset.Dependencies is not null)
        {
            field["dependencies"]
                .Children.AddRange(
                    manifestAsset.Dependencies.Select(x =>
                    {
                        // TODO: this is utter nonsense
                        return new AssetTypeValueField() { Value = new AssetTypeValue(x) };
                    })
                );
        }

        if (manifestAsset.Assets is not null)
        {
            field["assets"]
                .Children.AddRange(
                    manifestAsset.Assets.Select(x =>
                    {
                        return new AssetTypeValueField() { Value = new AssetTypeValue(x) };
                    })
                );
        }

        field["size"].AsLong = manifestAsset.Size;
        field["group"].AsInt = manifestAsset.Group;

        return field;
    }

    private static List<string> DeriveAssetList(string assetPath)
    {
        using AssetBundleHelper helper = AssetBundleHelper.FromPath(assetPath);

        var newElements = helper
            .GetContainerNames()
            .Select(containerName =>
                containerName
                    .Replace("assets/_gluonresources/", "", StringComparison.Ordinal)
                    .Replace("resources/", "", StringComparison.Ordinal)
            );

        return newElements.ToList();
    }

    private static void CreateDirectoryAndCopy(string bundlePath, string outputPath)
    {
        string outputDir =
            Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("Failed to get directory to create");
        Directory.CreateDirectory(outputDir);

        File.Copy(bundlePath, outputPath, overwrite: true);
    }

    private record struct ManifestAssetCollection(
        HashSet<ManifestAsset> MainAssets,
        HashSet<ManifestAsset> RawAssets
    );
}
