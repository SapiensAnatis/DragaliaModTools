using ModTools;
using ModTools.Commands;
using ModTools.Commands.Banner;
using ModTools.Commands.Manifest;

var app = ConsoleApp.Create();

ConsoleApp.JsonSerializerOptions = ModToolsSerializerContext.Default.Options;

app.UseFilter<ExceptionHandlerFilter>();

app.Add<CheckTargetCommand>();
app.Add<ConvertBundleCommand>();
app.Add<GetHashCommand>();
app.Add<ImportDictionaryCommand>();
app.Add<ImportMultipleDictionaryCommand>();
app.Add<BannerCommand>();

app.Add<DecryptCommand>("manifest");
app.Add<EditCommand>("manifest");
app.Add<MergeCommand>("manifest");
app.Add<VerifyCommand>("manifest");

await app.RunAsync(args).ConfigureAwait(false);
