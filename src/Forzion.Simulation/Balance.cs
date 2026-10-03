namespace Forzion.Simulation;

/// <summary>
/// Balance values, kept apart from the rules that use them so the game can be tuned without
/// touching logic.
/// </summary>
internal static class Balance
{
    /// <summary>Side of the Town Center's square footprint, in Cells. Odd, so it has a centre Cell.</summary>
    public const int TownCenterSize = 3;

    /// <summary>Villagers each Player starts with. No more than <see cref="TownCenterSize"/>: they line up along one of its sides.</summary>
    public const int StartingVillagers = 3;

    /// <summary>Sources of each Resource placed within reach of each Player's Town Center.</summary>
    public const int HomeSourcesPerResource = 2;

    /// <summary>How much a new source of the given Resource holds.</summary>
    public static int SourceAmount(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 300,
        ResourceKind.Wood => 300,
        ResourceKind.Gold => 400,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>How far a unit of the given kind walks, in Cells per second.</summary>
    public static Fix64 Speed(UnitKind kind) => kind switch
    {
        UnitKind.Villager => Fix64.FromInt(2),
        UnitKind.MeleeSoldier => Fix64.FromInt(2),
        UnitKind.RangedSoldier => Fix64.FromInt(2),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Hit points of a unit of the given kind when it is whole.</summary>
    public static int HitPoints(UnitKind kind) => kind switch
    {
        UnitKind.Villager => 25,
        UnitKind.MeleeSoldier => 45,
        UnitKind.RangedSoldier => 30,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Hit points of a building of the given kind when it is whole.</summary>
    public static int HitPoints(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => 600,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>How a unit of the given kind fights, or null when it cannot attack.</summary>
    public static AttackStats? Attack(UnitKind kind) => kind switch
    {
        UnitKind.Villager => null,
        UnitKind.MeleeSoldier => new AttackStats(
            Damage: 6, Range: Fix64.One, IntervalTicks: 20, PerceptionRadius: Fix64.FromInt(6)),
        UnitKind.RangedSoldier => new AttackStats(
            Damage: 4, Range: Fix64.FromInt(5), IntervalTicks: 30, PerceptionRadius: Fix64.FromInt(7)),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>The most a Villager carries at once.</summary>
    public const int VillagerCarryCapacity = 10;

    /// <summary>
    /// How far from a depleted source its Villagers look for another source of the same
    /// Resource, in Cells. Reaches across a home clearing, so a Player's sources beside its
    /// Town Center always find one another.
    /// </summary>
    public const int SourceSearchRadius = 15;

    /// <summary>Ticks a Villager spends gathering one unit of the given Resource.</summary>
    public static int GatherTicksPerUnit(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 10,
        ResourceKind.Wood => 12,
        ResourceKind.Gold => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}

/// <summary>How a unit fights.</summary>
/// <param name="Damage">Hit points one hit takes from the target.</param>
/// <param name="Range">
/// Farthest the target may be for a hit, in Cells: from the attacker's position to the
/// target's position, or to the nearest point of a building's footprint.
/// </param>
/// <param name="IntervalTicks">Ticks spent within range for each hit, which lands at the end of them.</param>
/// <param name="PerceptionRadius">How far, in Cells, an idle unit notices an enemy unit and attacks it.</param>
internal sealed record AttackStats(int Damage, Fix64 Range, int IntervalTicks, Fix64 PerceptionRadius);
