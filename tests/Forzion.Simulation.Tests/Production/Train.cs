using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

/// <summary>Helpers of the production tests, built on the simulation's public interface only.</summary>
internal static class Train
{
    public static BuildingState TownCenter(Match match, PlayerId player) =>
        match.State.Buildings.First(building => building.Owner == player && building.Kind == BuildingKind.TownCenter);

    /// <summary>
    /// A complete building of the given kind of the Player: its Town Center, or a building
    /// placed near it and built by all of its Villagers, who then stand idle beside it.
    /// </summary>
    public static BuildingState Complete(Match match, PlayerId player, BuildingKind kind)
    {
        if (kind == BuildingKind.TownCenter)
        {
            return TownCenter(match, player);
        }

        var villagers = Site.VillagersOf(match, player).Select(villager => villager.Id).ToList();
        var building = Site.Place(match, player, kind, villagers);
        TestMatches.TickUntil(match, () => building.IsComplete);

        return building;
    }

    /// <summary>How much of each Resource the Player holds, in <see cref="ResourceKind"/> order.</summary>
    public static List<int> Stock(PlayerState player) => Enum.GetValues<ResourceKind>().Select(player.AmountOf).ToList();

    /// <summary>Ticks the match the given number of times and returns every event with the tick it happened in.</summary>
    public static List<(int Tick, MatchEvent Event)> Run(Match match, int ticks)
    {
        var happened = new List<(int Tick, MatchEvent Event)>();

        for (var tick = 0; tick < ticks; tick++)
        {
            match.Tick();
            happened.AddRange(match.Events.Select(matchEvent => (match.State.Tick, matchEvent)));
        }

        return happened;
    }

    /// <summary>
    /// Prepares two matches alike with <paramref name="prepare"/>, sends the command it returns
    /// in one of them only, and checks that it was rejected for <paramref name="reason"/> and
    /// left the two matches with the same hash. The matches are made by <paramref name="create"/>,
    /// or are default two-Player matches without it.
    /// </summary>
    public static void AssertRejected(RejectionReason reason, Func<Match, Command> prepare, Func<Match>? create = null)
    {
        create ??= () => TestMatches.TwoPlayerMatch();
        var withRejection = create();
        var without = create();
        var command = prepare(withRejection);
        prepare(without);
        withRejection.Enqueue(command);

        withRejection.Tick();
        without.Tick();

        Assert.Equal([new CommandRejected(command, reason)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
    }
}
