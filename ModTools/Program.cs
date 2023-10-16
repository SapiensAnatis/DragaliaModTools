using AssetsTools.NET;
using SerializableDictionaryPlugin;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Runtime.CompilerServices;
using ModTools.Commands;

namespace ModTools;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("Serializable dictionary helper.");

        rootCommand.AddCommand(new ImportDictionaryCommand());

        return await rootCommand.InvokeAsync(args);
    }
}
