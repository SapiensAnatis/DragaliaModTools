using AssetsTools.NET;
using SerializableDictionaryPlugin;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Data;
using System.Runtime.CompilerServices;
using ModTools.Commands;

namespace ModTools;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("Dragalia modding utility.");

        rootCommand.AddCommand(new ImportDictionaryCommand());
        rootCommand.AddCommand(new ImportMultipleDictionaryCommand());
        rootCommand.AddCommand(new GetHashCommand());

        return await rootCommand.InvokeAsync(args);
    }
}
