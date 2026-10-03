using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

/// <summary>Matches with AI Players, and running them, through the public interface only.</summary>
internal static class AiMatches
{
    /// <summary>The two-Player configuration of <see cref="TestMatches"/>, with either Player as AI.</summary>
    public static MatchConfig Config(bool firstIsAi, bool secondIsAi, ulong seed = 42) =>
        new(
            seed,
            new MapConfig(64, 48),
            [
                new PlayerConfig(TestMatches.FirstFaction, IsAi: firstIsAi),
                new PlayerConfig(TestMatches.FirstFaction, IsAi: secondIsAi),
            ]);

    /// <summary>The Player's buildings of the given kind, construction sites included, in ascending ID order.</summary>
    public static IEnumerable<BuildingState> Buildings(Match match, PlayerId player, BuildingKind kind) =>
        match.State.Buildings.Where(building => building.Owner == player && building.Kind == kind);

    /// <summary>
    /// Ticks the match until the condition holds, and fails the test when it still does not
    /// after <paramref name="limit"/> ticks. Returns the ticks it took.
    /// </summary>
    public static int TickUntil(Match match, Func<bool> condition, int limit)
    {
        for (var ticks = 1; ticks <= limit; ticks++)
        {
            match.Tick();

            if (condition())
            {
                return ticks;
            }
        }

        Assert.Fail($"The condition did not hold within {limit} ticks.");

        return limit;
    }

    /// <summary>Ticks the match the given number of times and returns the events of all those ticks, in order.</summary>
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

    /// <summary>The Player's army: its units that are not Villagers, in ascending ID order.</summary>
    public static IEnumerable<UnitState> Army(Match match, PlayerId player) =>
        match.State.UnitsOf(player).Where(unit => unit.Kind != UnitKind.Villager);

    /// <summary>The Player's Town Center.</summary>
    public static BuildingState TownCenter(Match match, PlayerId player) =>
        match.State.Buildings.Single(building => building.Owner == player && building.Kind == BuildingKind.TownCenter);
}
