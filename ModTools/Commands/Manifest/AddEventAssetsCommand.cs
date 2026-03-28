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

        foreach (ManifestAsset asset in uniqueEventAssets.MainAssets.Values)
        {
            string bundlePath = Path.Combine(assetsDir, GetAssetPath(asset.Hash));

            ConsoleApp.LogVerbose($"Adding asset: {asset.Name}");

            AssetBundleHelper? openedBundle = null;

            if (conversion)
            {
                openedBundle = AssetBundleHelper.FromPath(bundlePath);
                BundleConversionHelper.ConvertToIos(openedBundle);
            }

            // May implement further modifications to the asset in a pipeline...

            if (openedBundle != null)
            {
                // Changes were made to the asset
                // Need to write + compress to a temporary file to calculate the new hash, as the hash is of a
                // compressed file. Could compress to a MemoryStream, hash, then write, but eh

                string newHash;
                string tempFilePath = Path.GetTempFileName();
                await using (FileStream tempFs = File.OpenWrite(tempFilePath))
                {
                    openedBundle.Write(tempFs);
                }

                openedBundle.Dispose();

                await using (FileStream tempReadFs = File.OpenRead(tempFilePath))
                {
                    newHash = HashHelper.GetHash(tempReadFs);
                }

                string outputPath = Path.Combine(outputBundleDir, GetAssetPath(newHash));
                CreateOutputDirectory(outputPath);
                File.Move(tempFilePath, outputPath, overwrite: true);

                AddAssetToManifest(targetBaseField, asset with { Hash = newHash }, bundlePath);
            }
            else
            {
                string outputPath = Path.Combine(outputBundleDir, GetAssetPath(asset.Hash));
                CreateOutputDirectory(outputPath);
                File.Copy(bundlePath, outputPath, overwrite: true);

                AddAssetToManifest(targetBaseField, asset, bundlePath);
            }
        }

        foreach (ManifestAsset asset in uniqueEventAssets.RawAssets.Values)
        {
            string bundlePath = Path.Combine(assetsDir, GetAssetPath(asset.Hash));
            string outputPath = Path.Combine(outputBundleDir, GetAssetPath(asset.Hash));

            CreateOutputDirectory(outputPath);
            File.Copy(bundlePath, outputPath, overwrite: true);

            AddRawAssetToManifest(targetBaseField, asset);
        }

        ValidateNoMissingDependenciesFinal(targetBaseField);

        targetHelper.UpdateBaseField("manifest", targetBaseField);

        string resultOutputPath = Path.Combine(outputManifestDir, Path.GetFileName(target));

        MemoryStream ms = new();
        targetHelper.Write(ms);

        byte[] decrypted = ms.ToArray();
        byte[] encrypted = RijndaelHelper.Encrypt(decrypted);

        ConsoleApp.Log($"[INFO] Writing output to {resultOutputPath}");
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
            others.ToDictionary(x => x.Name, x => x),
            raws.ToDictionary(x => x.Name, x => x)
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

        ConsoleApp.LogVerbose($"Event {eventId} ran between {preEventManifest} | {eventManifest}");

        var preEventAssets = await GetAssets(parsedManifestsDir, preEventManifest, locale);
        var eventAssets = await GetAssets(parsedManifestsDir, eventManifest, locale);
        var currentAssets = await GetAssets(
            parsedManifestsDir,
            "20221002_y2XM6giU6zz56wCm",
            locale
        ); // Assumes you are adding assets to the newest manifest

        Dictionary<string, ManifestAsset> resultMainAssets = [];
        Dictionary<string, ManifestAsset> resultRawAssets = [];

        foreach (var asset in eventAssets.MainAssets.Values)
        {
            if (
                !preEventAssets.MainAssets.ContainsKey(asset.Name)
                && !currentAssets.MainAssets.ContainsKey(asset.Name)
            )
            {
                resultMainAssets.Add(asset.Name, asset);
            }
        }

        foreach (var asset in eventAssets.RawAssets.Values)
        {
            if (
                !preEventAssets.RawAssets.ContainsKey(asset.Name)
                && !currentAssets.RawAssets.ContainsKey(asset.Name)
            )
            {
                resultRawAssets.Add(asset.Name, asset);
            }
        }

        resultMainAssets = DependencyHelper.AddMissingDependencies(
            currentAssets.MainAssets,
            eventAssets.MainAssets,
            resultMainAssets
        );

        return new ManifestAssetCollection(resultMainAssets, resultRawAssets);
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
            updatedAsset
        );
        array.Children.Add(childToAdd);
    }

    private static void AddRawAssetToManifest(
        AssetTypeValueField targetField,
        ManifestAsset assetToAdd
    )
    {
        AssetTypeValueField? array = targetField["rawAssets"]["Array"];

        AssetTypeValueField childToAdd = CreateFieldFromAssetModel(array.TemplateField, assetToAdd);
        array.Children.Add(childToAdd);
    }

    private static string GetAssetPath(string hash)
    {
        return Path.Join(hash[..2], hash);
    }

    private static AssetTypeValueField CreateFieldFromAssetModel(
        AssetTypeTemplateField arrayTemplate,
        ManifestAsset manifestAsset
    )
    {
        AssetTypeValueField? field = ValueBuilder.DefaultValueFieldFromArrayTemplate(arrayTemplate);

        field["name"].AsString = manifestAsset.Name;
        field["hash"].AsString = manifestAsset.Hash;

        if (manifestAsset.Dependencies is not null)
        {
            AssetTypeValueField? dependenciesArray = field["dependencies"]["Array"];
            Debug.Assert(dependenciesArray is { IsDummy: false, TemplateField.IsArray: true });

            dependenciesArray.Children.AddRange(
                manifestAsset.Dependencies.Select(x =>
                {
                    AssetTypeValueField? value = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                        dependenciesArray
                    );
                    value.AsString = x;
                    return value;
                })
            );
        }

        if (manifestAsset.Assets is not null)
        {
            AssetTypeValueField? assetsArray = field["assets"]["Array"];
            Debug.Assert(assetsArray is { IsDummy: false, TemplateField.IsArray: true });

            assetsArray.Children.AddRange(
                manifestAsset.Assets.Select(x =>
                {
                    AssetTypeValueField? value = ValueBuilder.DefaultValueFieldFromArrayTemplate(
                        assetsArray
                    );
                    value.AsString = x;
                    return value;
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

    private static void CreateOutputDirectory(string outputPath)
    {
        string outputDir =
            Path.GetDirectoryName(outputPath)
            ?? throw new InvalidOperationException("Failed to get directory to create");
        Directory.CreateDirectory(outputDir);
    }

    private static void ValidateNoMissingDependenciesFinal(AssetTypeValueField manifestField)
    {
        AssetTypeValueField? array = manifestField["categories"]["Array"][1]["assets"]["Array"];

        var assets = array.Select(x => x["name"].AsString).ToHashSet();
        var dependencies = array
            .SelectMany(x => x["dependencies"]["Array"].Children.Select(y => y.AsString))
            .ToHashSet();

        dependencies.ExceptWith(assets);

        if (dependencies.Count != 0)
        {
            throw new InvalidOperationException("Found missing dependencies!!!!!");
        }
    }

    private record struct ManifestAssetCollection(
        Dictionary<string, ManifestAsset> MainAssets,
        Dictionary<string, ManifestAsset> RawAssets
    );
}
