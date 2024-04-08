using System.CommandLine;
using System.Text.Json;
using AssetsTools.NET;
using ModTools.Shared;
using static SerializableDictionaryPlugin.Shared.SerializableDictionaryHelper;

namespace ModTools.Commands.Banner;

internal sealed class BannerCommand : Command
{
    private const string CommandName = "banner";

    private const string CommandDescription =
        "Populate a master asset with information from a banner.json configuration.";

    public BannerCommand()
        : base(CommandName, CommandDescription)
    {
        Argument<FileInfo> banner = new("banner", "Path to the banner.json file.");
        Option<FileInfo> masterSource =
            new("--source", "Path to the master asset to update.") { IsRequired = true };
        Option<FileInfo> output =
            new("--output", "Path to write the result to.") { IsRequired = true };

        this.AddArgument(banner);
        this.AddOption(masterSource);
        this.AddOption(output);

        AssetBundleHelperBinder sourceBinder = new(masterSource);

        this.SetHandler(DoCommand, banner, sourceBinder, output);
    }

    private static void DoCommand(
        FileInfo bannerPath,
        AssetBundleHelper masterSource,
        FileInfo outputPath
    )
    {
        AssetTypeValueField summonData = masterSource.GetBaseField("SummonData");
        Dictionary<int, SummonData> summonDataDict = LoadAsDictionary<int, SummonData>(summonData);

        AssetTypeValueField summonPointData = masterSource.GetBaseField("SummonPointData");
        Dictionary<int, SummonPointData> summonPointDict = LoadAsDictionary<int, SummonPointData>(
            summonPointData
        );

        BannerConfigFile bannerOptions;
        using (FileStream fs = File.OpenRead(bannerPath.FullName))
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
            Console.WriteLine($"Updating entries for banner ID {configBanner.Id}");
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
                assetPointData.CommenceDate = DateTimeHelper.FormatDate(configBanner.Start);
                assetPointData.CompleteDate = DateTimeHelper.FormatDate(configBanner.End);
            }
        }

        UpdateFromDictionary(summonData, summonDataDict);
        UpdateFromDictionary(summonPointData, summonPointDict);

        masterSource.UpdateBaseField("SummonData", summonData);
        masterSource.UpdateBaseField("SummonPointData", summonPointData);

        Console.WriteLine($"Saving result to {outputPath.FullName}");
        using (FileStream fs = outputPath.OpenWrite())
        {
            masterSource.Write(fs);
        }
    }
}
