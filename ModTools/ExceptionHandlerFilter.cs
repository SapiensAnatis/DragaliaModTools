namespace ModTools;

internal sealed class ExceptionHandlerFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            ConsoleApp.LogError($"Failed to open file at {ex.FileName}.");
            Environment.ExitCode = 1;
        }
#pragma warning disable CA1031 // Do not catch general exception types. App is about to exit at this point. We catch only to avoid dumping a stack trace.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            ConsoleApp.LogError($"Unhandled exception: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
