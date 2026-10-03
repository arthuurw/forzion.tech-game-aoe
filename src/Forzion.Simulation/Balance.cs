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

    /// <summary>
    /// Nearest a home resource source lies to the Cell its Town Center is centred on, in king's
    /// moves. At least 3, which keeps the sources clear of the Town Center and of the Villagers
    /// beside it.
    /// </summary>
    public const int NearestHomeSource = 3;

    /// <summary>
    /// Farthest a home resource source lies from the Cell its Town Center is centred on, in
    /// king's moves. Less than the radius of the clearing the map generator keeps around each
    /// home, so the sources stay inside it.
    /// </summary>
    public const int FarthestHomeSource = 5;

    /// <summary>Cells of map area per attempt to scatter a pair of resource sources away from the homes.</summary>
    public const int CellsPerFarSource = 512;

    /// <summary>Cells of map area per obstacle, forest or water, scattered away from the homes.</summary>
    public const int CellsPerObstacle = 128;

    /// <summary>
    /// Fewest steps of the random walk that lays an obstacle: every Cell the walk steps on
    /// becomes forest or water.
    /// </summary>
    public const int ShortestObstacleWalk = 16;

    /// <summary>
    /// How many walk lengths an obstacle can have: from <see cref="ShortestObstacleWalk"/> up to
    /// <see cref="ShortestObstacleWalk"/> + <see cref="ObstacleWalkSpread"/> - 1 steps.
    /// </summary>
    public const int ObstacleWalkSpread = 33;

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

    /// <summary>Ticks a Villager spends gathering one unit of the given Resource.</summary>
    public static int GatherTicksPerUnit(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 10,
        ResourceKind.Wood => 12,
        ResourceKind.Gold => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
