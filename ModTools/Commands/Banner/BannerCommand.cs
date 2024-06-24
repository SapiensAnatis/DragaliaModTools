using System.Text.Json;
using AssetsTools.NET;
using ModTools.Shared;
using static SerializableDictionaryPlugin.Shared.SerializableDictionaryHelper;

namespace ModTools.Commands.Banner;

internal sealed class BannerCommand
{
    /// <summary>
    /// Update the master asset with information from a banner.json configuration file.
    /// </summary>
    /// <param name="bannerPath">The path to the banner.json configuration.</param>
    /// <param name="masterPath">--master|-m The path to the master asset bundle to update.</param>
    /// <param name="outputPath">--output|-o The path to write the updated master asset bundle.</param>
    [Command("banner")]
    public void Command([Argument] string bannerPath, string masterPath, string outputPath)
    {
        using AssetBundleHelper masterSource = AssetBundleHelper.FromPath(masterPath);

        AssetTypeValueField summonData = masterSource.GetBaseField("SummonData");
        Dictionary<int, SummonData> summonDataDict = LoadAsDictionary<int, SummonData>(summonData);

        AssetTypeValueField summonPointData = masterSource.GetBaseField("SummonPointData");
        Dictionary<int, SummonPointData> summonPointDict = LoadAsDictionary<int, SummonPointData>(
            summonPointData
        );

        BannerConfigFile bannerOptions;
        using (FileStream fs = File.OpenRead(bannerPath))
        {
            bannerOptions =
                JsonSerializer.Deserialize(fs, ModToolsSerializerContext.Default.BannerConfigFile)
                ?? throw new JsonException("Failed to deserialize banner config");
        }

        int numBanners = bannerOptions.SummonBannerOptions.Banners.Count;

        foreach (
            (Banner configBanner, int index) in bannerOptions.SummonBannerOptions.Banners.Select(
                (x, index) => (x, index)
            )
        )
        {
            ConsoleApp.Log($"Updating entries for banner ID {configBanner.Id}");
            if (!summonDataDict.TryGetValue(configBanner.Id, out SummonData? assetBanner))
            {
                throw new NotSupportedException(
                    $"Could not find a SummonData entry with ID {configBanner.Id}. Adding new banners is not yet supported."
                );
            }

            // TODO: Look at PickupUnit values -- needs clarification on PickupResourceNum

            assetBanner.CommenceDate = DateTimeHelper.FormatDate(configBanner.Start);
            assetBanner.CompleteDate = DateTimeHelper.FormatDate(configBanner.End);

            // Highest priority shows first - calculate priority so that banners show in order of index in JSON array
            assetBanner.Priority = numBanners - index;

            // Set auto-play story ID
            assetBanner.EncounterStoryId = configBanner.EncounterStoryId ?? 0;

            assetBanner.ExchangeSummonPoint = 300;
            assetBanner.SummonPointId = configBanner.Id;

            if (!summonPointDict.TryGetValue(configBanner.Id, out SummonPointData? assetPointData))
            {
                summonPointDict[configBanner.Id] = new()
                {
                    Id = configBanner.Id,
                    SummonId = configBanner.Id,
                    CommenceDate = DateTimeHelper.FormatDate(configBanner.Start),
                    CompleteDate = DateTimeHelper.FormatDate(configBanner.End),
                };
            }
            else
            {
                assetPointData.SummonId = configBanner.Id;
                assetPointData.CommenceDate = DateTimeHelper.FormatDate(configBanner.Start);
                assetPointData.CompleteDate = DateTimeHelper.FormatDate(configBanner.End);
            }
        }

        UpdateFromDictionary(summonData, summonDataDict);
        UpdateFromDictionary(summonPointData, summonPointDict);

        masterSource.UpdateBaseField("SummonData", summonData);
        masterSource.UpdateBaseField("SummonPointData", summonPointData);

        ConsoleApp.Log($"Saving result to {outputPath}");
        using (FileStream fs = File.OpenWrite(outputPath))
        {
            masterSource.Write(fs);
        }
    }
}
