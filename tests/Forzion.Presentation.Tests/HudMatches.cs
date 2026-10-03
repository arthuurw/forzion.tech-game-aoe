using Forzion.Simulation;

namespace Forzion.Presentation.Tests;

/// <summary>Matches set up for the HUD tests, built and played through the public interface of <see cref="Match"/>.</summary>
internal static class HudMatches
{
    public static readonly PlayerId FirstPlayer = new(1);
    public static readonly PlayerId SecondPlayer = new(2);

    /// <summary>
    /// A made-up Faction of three Ages, each free and quick to reach: Age I unlocks Villagers
    /// and Houses, Age II the Storehouse, the Barracks and the melee and ranged soldiers, and
    /// Age III the heavy soldier. Its units have names only for the Villager.
    /// </summary>
    public static readonly Faction ThreeAges = new(
        new FactionId(7),
        "TEST_FACTION",
        [
            new FactionAge("TEST_AGE_1", new Cost(0, 0, 0), 0, [UnitKind.Villager], [BuildingKind.House]),
            new FactionAge(
                "TEST_AGE_2",
                new Cost(0, 0, 0),
                10,
                [UnitKind.MeleeSoldier, UnitKind.RangedSoldier],
                [BuildingKind.Storehouse, BuildingKind.Barracks]),
            new FactionAge("TEST_AGE_3", new Cost(0, 0, 0), 20, [UnitKind.HeavySoldier], []),
        ],
        new Dictionary<UnitKind, string> { [UnitKind.Villager] = "TEST_VILLAGER" });

    /// <summary>Two Players of the Portuguese on a 64 by 48 map.</summary>
    public static Match Portuguese(IReadOnlyList<StartingUnit>? firstExtras = null) => Match.Create(new MatchConfig(
        42,
        new MapConfig(64, 48),
        [new PlayerConfig(Factions.Portuguese.Id, firstExtras), new PlayerConfig(Factions.Portuguese.Id)]));

    /// <summary>Two Players of <see cref="ThreeAges"/> on a 64 by 48 map.</summary>
    public static Match OfThreeAges() => Match.Create(new MatchConfig(
        42,
        new MapConfig(64, 48),
        [new PlayerConfig(ThreeAges.Id), new PlayerConfig(ThreeAges.Id)],
        [ThreeAges]));

    /// <summary>
    /// The Cell two left of the centre of the first Player's Town Center: free, and away from
    /// the Villagers' row.
    /// </summary>
    public static CellPosition BesideFirstHome()
    {
        var townCenter = TownCenterOf(Portuguese(), FirstPlayer);

        return new CellPosition(townCenter.Origin.X + (townCenter.Width / 2) - 2, townCenter.Origin.Y + (townCenter.Height / 2));
    }

    /// <summary>
    /// An origin near the first Player's Town Center, searched outward, where a building of
    /// the given kind fits with a free Cell to spare all around it.
    /// </summary>
    public static CellPosition FreeOriginNearFirstHome(Match match, BuildingKind kind)
    {
        var home = TownCenterOf(match, FirstPlayer).Origin;
        var size = Match.BuildingSize(kind);

        for (var reach = 4; reach < 20; reach++)
        {
            for (var dy = -reach; dy <= reach; dy++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var origin = new CellPosition(home.X + dx, home.Y + dy);

                    if (match.CanPlace(kind, origin) && IsClearAround(match, origin, size))
                    {
                        return origin;
                    }
                }
            }
        }

        throw new InvalidOperationException("No free origin near the first Town Center.");
    }

    /// <summary>Whether the footprint and the ring of Cells around it are free: builders can walk all around.</summary>
    private static bool IsClearAround(Match match, CellPosition origin, int size)
    {
        for (var y = origin.Y - 1; y <= origin.Y + size; y++)
        {
            for (var x = origin.X - 1; x <= origin.X + size; x++)
            {
                var cell = new CellPosition(x, y);

                if (!match.State.Map.Contains(cell) || match.State.Map[cell] != CellKind.Free)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static BuildingState TownCenterOf(Match match, PlayerId player) =>
        match.State.Buildings.First(building => building.Owner == player && building.Kind == BuildingKind.TownCenter);

    public static List<UnitState> UnitsOf(Match match, PlayerId player) =>
        match.State.Units.Where(unit => unit.Owner == player).ToList();

    /// <summary>Runs the given number of ticks.</summary>
    public static void Run(Match match, int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            match.Tick();
        }
    }

    /// <summary>Orders the Age Advance of the Player and runs ticks until it reaches the next Age.</summary>
    public static void AdvanceAge(Match match, PlayerId player)
    {
        var age = match.State.Players[player.Value - 1].Age;
        match.Enqueue(new AgeAdvanceCommand(player, TownCenterOf(match, player).Id));

        while (match.State.Players[player.Value - 1].Age == age)
        {
            match.Tick();
        }
    }
}
