using System.Runtime.CompilerServices;
using System.Text;
// ReSharper disable once CheckNamespace
using ModTools.Shared;

namespace ConsoleAppFramework;

internal static partial class ConsoleApp
{
    public static void LogVerbose(VerboseLogInterpolatedStringHandler builder)
    {
        if (GlobalOptions.Instance.Verbose)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Log(builder.BuildString());
            Console.ResetColor();
        }
    }

    public static void LogVerbose(string msg)
    {
        if (GlobalOptions.Instance.Verbose)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Log(msg);
            Console.ResetColor();
        }
    }

    public static void LogWarning(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(msg);
        Console.ResetColor();
    }
}

[InterpolatedStringHandler]
internal struct VerboseLogInterpolatedStringHandler
{
    private StringBuilder builder;

    public VerboseLogInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        builder = new StringBuilder(literalLength);
    }

    public void AppendLiteral(string s)
    {
        if (!GlobalOptions.Instance.Verbose)
        {
            return;
        }

        builder.Append(s);
    }

    public void AppendFormatted<T>(T t)
    {
        if (!GlobalOptions.Instance.Verbose)
        {
            return;
        }

        builder.Append(t);
    }

    public string BuildString()
    {
        return builder.ToString();
    }
}
