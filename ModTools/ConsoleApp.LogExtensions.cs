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
            Log($"[DEBUG] {builder.BuildString()}");
        }
    }

    public static void LogVerbose(string msg)
    {
        if (GlobalOptions.Instance.Verbose)
        {
            Log($"[DEBUG] {msg}");
        }
    }

    public static void LogWarning(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine($"[WARN] {msg}");
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
