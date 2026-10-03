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

    /// <summary>How much of the given Resource each Player starts the match with.</summary>
    public static int StartingAmount(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 200,
        ResourceKind.Wood => 200,
        ResourceKind.Gold => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

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
        UnitKind.HeavySoldier => Fix64.FromInt(2),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Hit points of a unit of the given kind when it is whole.</summary>
    public static int HitPoints(UnitKind kind) => kind switch
    {
        UnitKind.Villager => 25,
        UnitKind.MeleeSoldier => 45,
        UnitKind.RangedSoldier => 30,
        UnitKind.HeavySoldier => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Hit points of a building of the given kind when it is whole.</summary>
    public static int HitPoints(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => 600,
        BuildingKind.House => 150,
        BuildingKind.Storehouse => 200,
        BuildingKind.Barracks => 300,
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
        UnitKind.HeavySoldier => new AttackStats(
            Damage: 10, Range: Fix64.One, IntervalTicks: 20, PerceptionRadius: Fix64.FromInt(6)),
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

    /// <summary>
    /// How much a complete building of the given kind adds to its Player's population limit:
    /// the Town Center gives the base and each House adds to it.
    /// </summary>
    public static int PopulationProvided(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => 5,
        BuildingKind.House => 5,
        BuildingKind.Storehouse => 0,
        BuildingKind.Barracks => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Whether buildings of the given kind train units.</summary>
    public static bool Trains(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => true,
        BuildingKind.House => false,
        BuildingKind.Storehouse => false,
        BuildingKind.Barracks => true,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>The kind of building that trains units of the given kind.</summary>
    public static BuildingKind TrainedAt(UnitKind kind) => kind switch
    {
        UnitKind.Villager => BuildingKind.TownCenter,
        UnitKind.MeleeSoldier => BuildingKind.Barracks,
        UnitKind.RangedSoldier => BuildingKind.Barracks,
        UnitKind.HeavySoldier => BuildingKind.Barracks,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>What training a unit of the given kind costs, paid in full when it joins a training queue.</summary>
    public static Cost UnitCost(UnitKind kind) => kind switch
    {
        UnitKind.Villager => new Cost(50, 0, 0),
        UnitKind.MeleeSoldier => new Cost(60, 0, 20),
        UnitKind.RangedSoldier => new Cost(0, 25, 45),
        UnitKind.HeavySoldier => new Cost(70, 0, 30),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Ticks a building spends training a unit of the given kind.</summary>
    public static int TrainTime(UnitKind kind) => kind switch
    {
        UnitKind.Villager => 15 * Match.TicksPerSecond,
        UnitKind.MeleeSoldier => 20 * Match.TicksPerSecond,
        UnitKind.RangedSoldier => 18 * Match.TicksPerSecond,
        UnitKind.HeavySoldier => 25 * Match.TicksPerSecond,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>What the Portuguese Age Advance to Age II costs.</summary>
    public static readonly Cost SecondAgeAdvanceCost = new(300, 0, 100);

    /// <summary>Ticks the Portuguese Age Advance to Age II takes.</summary>
    public const int SecondAgeAdvanceTime = 40 * Match.TicksPerSecond;

    /// <summary>Ticks a Villager spends gathering one unit of the given Resource.</summary>
    public static int GatherTicksPerUnit(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 10,
        ResourceKind.Wood => 12,
        ResourceKind.Gold => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// The AI's share of Villagers for each Resource: an idle Villager goes to the Resource
    /// whose gatherers are fewest for its share.
    /// </summary>
    public static int AiGatherShare(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => 5,
        ResourceKind.Wood => 3,
        ResourceKind.Gold => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Among how many of the sources of a Resource nearest its Town Center the AI draws the one
    /// an idle Villager goes to. More than one keeps a Villager that cannot reach a source from
    /// being sent back to it every time.
    /// </summary>
    public const int AiSourceChoices = 3;

    /// <summary>How many Villagers the AI trains, one at a time, before it stops.</summary>
    public const int AiVillagers = 12;

    /// <summary>
    /// How close the AI lets its population come to the population limit before it places a
    /// House: once no more than this many units are left to train, it places one.
    /// </summary>
    public const int AiPopulationHeadroom = 2;

    /// <summary>How many Villagers the AI has before it places its Barracks.</summary>
    public const int AiVillagersBeforeBarracks = 6;

    /// <summary>
    /// How large an army the AI trains before it saves up for its Age Advance. Until the
    /// advance is paid, it trains more soldiers only from what is left beyond its cost.
    /// </summary>
    public const int AiArmyBeforeAdvance = 3;

    /// <summary>How many Villagers the AI sends to build a building of the given kind when it places it.</summary>
    public static int AiBuilders(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Town Centers are not placed."),
        BuildingKind.House => 1,
        BuildingKind.Storehouse => 1,
        BuildingKind.Barracks => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Farthest the AI places a building from the centre of its Town Center, in Cells along either axis.</summary>
    public const int AiBuildingReach = 10;

    /// <summary>Among how many of the places nearest its Town Center the AI draws the one a building goes to.</summary>
    public const int AiPlacementChoices = 3;
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
