using AssetsTools.NET;
using SerializableDictionaryPlugin;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Data;
using System.Runtime.CompilerServices;
using ModTools.Commands;
using ModTools.Commands.Manifest;

namespace ModTools;

internal class Program
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

        return await rootCommand.InvokeAsync(args);
    }
}
