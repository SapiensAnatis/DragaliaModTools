using ModTools;
using ModTools.Commands;
using ModTools.Commands.Banner;
using ModTools.Commands.Manifest;
using ModTools.Shared;

var app = ConsoleApp.Create();

ConsoleApp.JsonSerializerOptions = ModToolsSerializerContext.Default.Options;

app.UseFilter<ExceptionHandlerFilter>();
app.UseFilter<GlobalOptions.SetGlobalOptionsFilter>();

app.ConfigureGlobalOptions((ref builder) =>
{
    bool readFromDisk = builder.AddGlobalOption("--read-from-disk",
        "Whether to decrease memory usage, at the expense of performance, by reading bundles directly from disk without loading them into memory first.",
        false);
    return new GlobalOptions(readFromDisk);
});

app.Add<CheckTargetCommand>();
app.Add<ConvertBundleCommand>();
app.Add<GetHashCommand>();
app.Add<ImportDictionaryCommand>();
app.Add<ImportMultipleDictionaryCommand>();
app.Add<BannerCommand>();
app.Add<UpdateNamesCommand>();

app.Add<DecryptCommand>("manifest");
app.Add<EditCommand>("manifest");
app.Add<MergeCommand>("manifest");
app.Add<VerifyCommand>("manifest");
app.Add<ImportExportCommand>("manifest");
app.Add<AddBundleCommand>("manifest");

await app.RunAsync(args).ConfigureAwait(false);