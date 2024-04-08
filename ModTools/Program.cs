using System.CommandLine;
using System.CommandLine.Binding;
using System.Data;
using System.Runtime.CompilerServices;
using AssetsTools.NET;
using ModTools.Commands;
using ModTools.Commands.Banner;
using ModTools.Commands.Manifest;
using SerializableDictionaryPlugin;

namespace ModTools;

internal sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("Dragalia modding utility.");

        rootCommand.AddCommand(new ImportDictionaryCommand());
        rootCommand.AddCommand(new ImportMultipleDictionaryCommand());
        rootCommand.AddCommand(new GetHashCommand());
        rootCommand.AddCommand(new ManifestCommand());
        rootCommand.AddCommand(new ConvertBundleCommand());
        rootCommand.AddCommand(new CheckTargetCommand());
        rootCommand.AddCommand(new BannerCommand());

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }
}
