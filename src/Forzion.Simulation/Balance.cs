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

    /// <summary>Side of the square footprint of a building of the given kind, in Cells.</summary>
    public static int BuildingSize(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => TownCenterSize,
        BuildingKind.House => 2,
        BuildingKind.Storehouse => 2,
        BuildingKind.Barracks => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>What placing a building of the given kind costs. Town Centers are not placed and have none.</summary>
    public static Cost BuildingCost(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Town Centers are not placed."),
        BuildingKind.House => new Cost(0, 30, 0),
        BuildingKind.Storehouse => new Cost(0, 50, 0),
        BuildingKind.Barracks => new Cost(0, 100, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Ticks of one Villager's work a building of the given kind takes to build. Villagers
    /// building together each add their own work.
    /// </summary>
    public static int BuildTime(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => 60 * Match.TicksPerSecond,
        BuildingKind.House => 15 * Match.TicksPerSecond,
        BuildingKind.Storehouse => 20 * Match.TicksPerSecond,
        BuildingKind.Barracks => 30 * Match.TicksPerSecond,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Ticks a Villager spends gathering one unit of the given Resource.</summary>
    public static int GatherTicksPerUnit(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 10,
        ResourceKind.Wood => 12,
        ResourceKind.Gold => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
