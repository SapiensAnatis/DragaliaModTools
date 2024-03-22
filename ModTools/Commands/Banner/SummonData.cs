#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace ModTools.Commands.Banner;

internal sealed class SummonData
{
    public int Id { get; set; }

    public int Priority { get; set; }

    public int SummonType { get; set; }

    public required string SummonViewName { get; set; }

    public int MaxSummonQuantity { get; set; }

    public required string CommenceDate { get; set; }

    public required string CompleteDate { get; set; }

    public int SummonPointId { get; set; }

    public int ExchangeSummonPoint { get; set; }

    public int GuaranteedEntityType { get; set; }

    public int IsPickupGuaranteed { get; set; }

    public int EncounterStoryId { get; set; }

    public required string SummonBgm { get; set; }

    public int PickupNum { get; set; }

    public int IsViewCover { get; set; }

    public int PickupUnitType1 { get; set; }

    public int PickupUnitId1 { get; set; }

    public int PickupResourceId1 { get; set; }

    public int PickupUnitType2 { get; set; }

    public int PickupUnitId2 { get; set; }

    public int PickupResourceId2 { get; set; }

    public int PickupUnitType3 { get; set; }

    public int PickupUnitId3 { get; set; }

    public int PickupResourceId3 { get; set; }

    public int PickupUnitType4 { get; set; }

    public int PickupUnitId4 { get; set; }

    public int PickupResourceId4 { get; set; }

    public required string MeetingStoryBanner { get; set; }
}
