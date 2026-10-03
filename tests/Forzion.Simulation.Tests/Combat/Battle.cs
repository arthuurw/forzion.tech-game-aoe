using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

/// <summary>Helpers of the combat tests, built on the simulation's public interface only.</summary>
internal static class Battle
{
    /// <summary>More ticks than any fight in the combat tests takes.</summary>
    public const int TickLimit = 20_000;

    // Two Cells to either side of the Town Center's centre: free, and away from the Villagers' row.
    private static readonly (int X, int Y)[] SiegeOffsets = [(-2, -1), (-2, 0), (-2, 1), (2, -1), (2, 0), (2, 1)];

    /// <summary>The middle one of the Player's starting Villagers, which stands beside the middle of the Town Center.</summary>
    public static UnitState MiddleVillager(Match match, PlayerId player) =>
        match.State.Units.Where(unit => unit.Owner == player && unit.Kind == UnitKind.Villager).ElementAt(1);

    public static BuildingState TownCenter(Match match, PlayerId player) =>
        match.State.Buildings.First(building => building.Owner == player);

    /// <summary>
    /// The default two-Player match, with each Player's extra units placed on Cells beside
    /// that Player's own Town Center or the other's, chosen from the plain match.
    /// </summary>
    public static Match Create(
        Func<Match, IReadOnlyList<StartingUnit>>? first = null,
        Func<Match, IReadOnlyList<StartingUnit>>? second = null,
        ulong seed = 42)
    {
        var plain = TestMatches.TwoPlayerMatch(seed);

        return Match.Create(TestArmies.Config(seed, first?.Invoke(plain), second?.Invoke(plain)));
    }

    /// <summary>
    /// A match in which the first Player starts with melee soldiers on both flanks of the
    /// second Player's Town Center, already ordered to attack it.
    /// </summary>
    public static Match Siege(ulong seed = 42)
    {
        var match = Create(
            first: plain => SiegeOffsets
                .Select(offset => new StartingUnit(
                    UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, TestMatches.SecondPlayer, offset.X, offset.Y)))
                .ToList(),
            seed: seed);

        match.Enqueue(new AttackCommand(
            TestMatches.FirstPlayer,
            Besiegers(match).Select(soldier => soldier.Id).ToList(),
            TownCenter(match, TestMatches.SecondPlayer).Id));

        return match;
    }

    /// <summary>
    /// The default match, with the second Player starting with three melee soldiers beside
    /// its own Town Center, far from the first Player's.
    /// </summary>
    public static Match Raiders() => Create(
        second: plain =>
        [
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, TestMatches.SecondPlayer, 2, -1)),
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, TestMatches.SecondPlayer, 2, 0)),
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, TestMatches.SecondPlayer, 2, 1)),
        ]);

    /// <summary>The second Player's soldiers of <see cref="Raiders"/> still standing.</summary>
    public static List<UnitState> RaidersOf(Match match) =>
        match.State.Units.Where(unit => unit.Owner == TestMatches.SecondPlayer && unit.Kind == UnitKind.MeleeSoldier).ToList();

    /// <summary>Orders the soldiers of <see cref="Raiders"/> to attack the building.</summary>
    public static void Raid(Match match, BuildingState building) =>
        match.Enqueue(new AttackCommand(
            TestMatches.SecondPlayer, RaidersOf(match).Select(unit => unit.Id).ToList(), building.Id));

    /// <summary>The first Player's soldiers of a <see cref="Siege"/> still standing.</summary>
    public static List<UnitState> Besiegers(Match match) =>
        match.State.Units.Where(unit => unit.Owner == TestMatches.FirstPlayer && unit.Kind == UnitKind.MeleeSoldier).ToList();

    /// <summary>The unit of the match with the given ID, or null once it is gone.</summary>
    public static UnitState? Unit(Match match, EntityId id) => match.State.Units.FirstOrDefault(unit => unit.Id == id);

    /// <summary>The building of the match with the given ID, or null once it is gone.</summary>
    public static BuildingState? Building(Match match, EntityId id) =>
        match.State.Buildings.FirstOrDefault(building => building.Id == id);

    /// <summary>The last unit of the match: the last extra unit of the last Player given any.</summary>
    public static UnitState Last(Match match) => match.State.Units[^1];

    /// <summary>Ticks the match until the condition holds and returns the ticks it took.</summary>
    public static int TickUntil(Match match, Func<bool> condition)
    {
        for (var tick = 1; tick <= TickLimit; tick++)
        {
            match.Tick();

            if (condition())
            {
                return tick;
            }
        }

        throw new InvalidOperationException($"The condition still did not hold after {TickLimit} ticks.");
    }

    /// <summary>Ticks the match the given number of times and returns the events of every tick, in order.</summary>
    public static List<MatchEvent> Run(Match match, int ticks)
    {
        var events = new List<MatchEvent>();

        for (var tick = 0; tick < ticks; tick++)
        {
            match.Tick();
            events.AddRange(match.Events);
        }

        return events;
    }

    /// <summary>Straight-line distance between two points, in Cells, as a double for the tests' own checks.</summary>
    public static double Distance(MapPosition a, MapPosition b) =>
        Math.Sqrt(Math.Pow(a.X.ToDouble() - b.X.ToDouble(), 2) + Math.Pow(a.Y.ToDouble() - b.Y.ToDouble(), 2));
}
