using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;

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
        Gather.Until(match, () => building.IsComplete);

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
}
