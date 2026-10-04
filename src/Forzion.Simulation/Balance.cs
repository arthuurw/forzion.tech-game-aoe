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

    private static readonly UnitStats Villager = new(
        Speed: Fix64.FromInt(2),
        HitPoints: 25,
        Attack: null,
        Cost: new Cost(50, 0, 0),
        TrainTime: 15 * Match.TicksPerSecond,
        TrainedAt: BuildingKind.TownCenter);

    private static readonly UnitStats MeleeSoldier = new(
        Speed: Fix64.FromInt(2),
        HitPoints: 45,
        Attack: new AttackStats(Damage: 6, Range: Fix64.One, AttackInterval: 20, PerceptionRadius: Fix64.FromInt(6)),
        Cost: new Cost(60, 0, 20),
        TrainTime: 20 * Match.TicksPerSecond,
        TrainedAt: BuildingKind.Barracks);

    private static readonly UnitStats RangedSoldier = new(
        Speed: Fix64.FromInt(2),
        HitPoints: 30,
        Attack: new AttackStats(Damage: 4, Range: Fix64.FromInt(5), AttackInterval: 30, PerceptionRadius: Fix64.FromInt(7)),
        Cost: new Cost(0, 25, 45),
        TrainTime: 18 * Match.TicksPerSecond,
        TrainedAt: BuildingKind.Barracks);

    private static readonly UnitStats HeavySoldier = new(
        Speed: Fix64.FromInt(2),
        HitPoints: 80,
        Attack: new AttackStats(Damage: 10, Range: Fix64.One, AttackInterval: 20, PerceptionRadius: Fix64.FromInt(6)),
        Cost: new Cost(70, 0, 30),
        TrainTime: 25 * Match.TicksPerSecond,
        TrainedAt: BuildingKind.Barracks);

    private static readonly BuildingStats TownCenter = new(
        Size: TownCenterSize, HitPoints: 600, Cost: null, BuildTime: 60 * Match.TicksPerSecond, PopulationProvided: 5, Trains: true);

    private static readonly BuildingStats House = new(
        Size: 2, HitPoints: 150, Cost: new Cost(0, 30, 0), BuildTime: 15 * Match.TicksPerSecond, PopulationProvided: 5, Trains: false);

    private static readonly BuildingStats Storehouse = new(
        Size: 2, HitPoints: 200, Cost: new Cost(0, 50, 0), BuildTime: 20 * Match.TicksPerSecond, PopulationProvided: 0, Trains: false);

    private static readonly BuildingStats Barracks = new(
        Size: 3, HitPoints: 300, Cost: new Cost(0, 100, 0), BuildTime: 30 * Match.TicksPerSecond, PopulationProvided: 0, Trains: true);

    private static readonly ResourceStats Food = new(StartingAmount: 200, SourceAmount: 300, GatherTicksPerUnit: 10);

    private static readonly ResourceStats Wood = new(StartingAmount: 200, SourceAmount: 300, GatherTicksPerUnit: 12);

    private static readonly ResourceStats Gold = new(StartingAmount: 100, SourceAmount: 400, GatherTicksPerUnit: 16);

    /// <summary>The balance values of a unit of the given kind.</summary>
    public static UnitStats Of(UnitKind kind) => kind switch
    {
        UnitKind.Villager => Villager,
        UnitKind.MeleeSoldier => MeleeSoldier,
        UnitKind.RangedSoldier => RangedSoldier,
        UnitKind.HeavySoldier => HeavySoldier,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>The balance values of a building of the given kind.</summary>
    public static BuildingStats Of(BuildingKind kind) => kind switch
    {
        BuildingKind.TownCenter => TownCenter,
        BuildingKind.House => House,
        BuildingKind.Storehouse => Storehouse,
        BuildingKind.Barracks => Barracks,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>The balance values of the given Resource.</summary>
    public static ResourceStats Of(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => Food,
        ResourceKind.Wood => Wood,
        ResourceKind.Gold => Gold,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>What the Portuguese Age Advance to Age II costs.</summary>
    public static readonly Cost SecondAgeAdvanceCost = new(300, 0, 100);

    /// <summary>Ticks the Portuguese Age Advance to Age II takes.</summary>
    public const int SecondAgeAdvanceTime = 40 * Match.TicksPerSecond;

    /// <summary>The most a Villager carries at once.</summary>
    public const int VillagerCarryCapacity = 10;

    /// <summary>
    /// How far from a depleted source its Villagers look for another source of the same
    /// Resource, in Cells. Reaches across a home clearing, so a Player's sources beside its
    /// Town Center always find one another.
    /// </summary>
    public const int SourceSearchRadius = 15;
}

/// <summary>The balance values of a kind of unit.</summary>
/// <param name="Speed">How far the unit walks, in Cells per second.</param>
/// <param name="HitPoints">Hit points of the unit when it is whole.</param>
/// <param name="Attack">How the unit fights, or null when it cannot attack.</param>
/// <param name="Cost">What training the unit costs, paid in full when it joins a training queue.</param>
/// <param name="TrainTime">Ticks a building spends training the unit.</param>
/// <param name="TrainedAt">The kind of building that trains the unit.</param>
internal sealed record UnitStats(Fix64 Speed, int HitPoints, AttackStats? Attack, Cost Cost, int TrainTime, BuildingKind TrainedAt);

/// <summary>The balance values of a kind of building.</summary>
/// <param name="Size">Side of the building's square footprint, in Cells.</param>
/// <param name="HitPoints">Hit points of the building when it is whole.</param>
/// <param name="Cost">
/// What placing the building costs, or null when Players do not place it: each Player starts
/// with its Town Center and never places another.
/// </param>
/// <param name="BuildTime">
/// Ticks of one Villager's work the building takes to build. Villagers building together each
/// add their own work.
/// </param>
/// <param name="PopulationProvided">
/// How much the complete building adds to its Player's population limit: the Town Center gives
/// the base and each House adds to it.
/// </param>
/// <param name="Trains">Whether the complete building trains units.</param>
internal sealed record BuildingStats(int Size, int HitPoints, Cost? Cost, int BuildTime, int PopulationProvided, bool Trains);

/// <summary>The balance values of a Resource.</summary>
/// <param name="StartingAmount">How much of the Resource each Player starts the match with.</param>
/// <param name="SourceAmount">How much a new source of the Resource holds.</param>
/// <param name="GatherTicksPerUnit">Ticks a Villager spends gathering one unit of the Resource.</param>
internal sealed record ResourceStats(int StartingAmount, int SourceAmount, int GatherTicksPerUnit);

/// <summary>How a unit fights.</summary>
/// <param name="Damage">Hit points one hit takes from the target.</param>
/// <param name="Range">
/// Farthest the target may be for a hit, in Cells: from the attacker's position to the
/// target's position, or to the nearest point of a building's footprint.
/// </param>
/// <param name="AttackInterval">Ticks spent within range for each hit, which lands at the end of them.</param>
/// <param name="PerceptionRadius">How far, in Cells, an idle unit notices an enemy unit or building and attacks it.</param>
internal sealed record AttackStats(int Damage, Fix64 Range, int AttackInterval, Fix64 PerceptionRadius);
