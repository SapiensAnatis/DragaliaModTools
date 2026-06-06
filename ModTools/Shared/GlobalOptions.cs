using System.Diagnostics;

namespace ModTools.Shared;

internal sealed record GlobalOptions(bool ReadFromDisk, bool Verbose)
{
    public static GlobalOptions Instance { get; private set; } = null!;

    internal sealed class SetGlobalOptionsFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        private readonly ConsoleAppFilter next = next;

        public override Task InvokeAsync(
            ConsoleAppContext context,
            CancellationToken cancellationToken
        )
        {
            Instance =
                (GlobalOptions?)context.GlobalOptions
                ?? throw new UnreachableException("Global options not initialised");

            return this.next.InvokeAsync(context, cancellationToken);
        }
    }
};
