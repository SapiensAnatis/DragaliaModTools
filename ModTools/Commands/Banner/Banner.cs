using System.Text.Json.Serialization;

namespace ModTools.Commands.Banner;

internal sealed class Banner
{
    public int Id { get; init; }
    
    public int Priority { get; init; }

    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }

    public bool IsGala { get; init; }

    public bool IsPrizeShowcase { get; init; }

    public IReadOnlyList<Charas> PickupCharas { get; init; } = [];

    public IReadOnlyList<Dragons> PickupDragons { get; init; } = [];
}
