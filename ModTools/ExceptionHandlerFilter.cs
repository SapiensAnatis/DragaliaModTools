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
        catch (Exception ex)
        {
            ConsoleApp.LogError($"Unhandled exception: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
