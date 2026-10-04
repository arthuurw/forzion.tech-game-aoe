namespace Forzion.Simulation.Tests.Matches;

/// <summary>Match configurations shared by the tests of the simulation's public interface.</summary>
internal static class TestMatches
{
    /// <summary>More ticks than any test waits for a condition.</summary>
    public const int TickLimit = 20_000;

    /// <summary>Twenty minutes of play: more ticks than any whole match of the tests takes to end.</summary>
    public const int WholeMatchLimit = 20 * 60 * Match.TicksPerSecond;

    public static readonly FactionId FirstFaction = new(1);

    public static readonly PlayerId FirstPlayer = new(1);

    public static readonly PlayerId SecondPlayer = new(2);

    /// <summary>The default two-Player match on a 64 by 48 map, with extra units for either Player.</summary>
    public static MatchConfig TwoPlayerConfig(
        ulong seed = 42, IReadOnlyList<StartingUnit>? first = null, IReadOnlyList<StartingUnit>? second = null) =>
        new(seed, new MapConfig(64, 48), [new PlayerConfig(FirstFaction, first), new PlayerConfig(FirstFaction, second)]);

    public static Match TwoPlayerMatch(ulong seed = 42) => Match.Create(TwoPlayerConfig(seed));

    /// <summary>A match of the first Player alone, on the same map size as <see cref="TwoPlayerConfig"/>.</summary>
    public static MatchConfig SinglePlayerConfig(ulong seed = 1) =>
        new(seed, new MapConfig(64, 48), [new PlayerConfig(FirstFaction)]);

    /// <summary>The middle one of the first Player's starting Villagers, which stands beside the middle of the Town Center.</summary>
    public static UnitState MiddleVillager(Match match) => MiddleVillager(match, FirstPlayer);

    /// <summary>The middle one of the Player's starting Villagers, which stands beside the middle of the Town Center.</summary>
    public static UnitState MiddleVillager(Match match, PlayerId player) =>
        match.State.UnitsOf(player).Where(unit => unit.Kind == UnitKind.Villager).ElementAt(1);

    /// <summary>
    /// Ticks the match until the condition holds and returns the ticks it took, failing the
    /// test after <see cref="TickLimit"/> ticks.
    /// </summary>
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

    /// <summary>The Player's units, in ascending ID order.</summary>
    public static IEnumerable<UnitState> UnitsOf(this MatchState state, PlayerId player) =>
        state.Units.Where(unit => unit.Owner == player);
}
