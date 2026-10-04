using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Production;

namespace Forzion.Simulation.Tests.Ages;

/// <summary>Helpers of the Age tests, built on the simulation's public interface only.</summary>
internal static class Advance
{
    /// <summary>
    /// Has every Villager of the Player gather each Resource the Player has less of than the
    /// cost asks for, one Resource after another, then stops them where they stand.
    /// </summary>
    public static void Afford(Match match, PlayerId player, Cost cost)
    {
        var state = match.State;
        var holder = state.Players[player.Value - 1];
        var villagers = Site.VillagersOf(match, player);

        foreach (var kind in Enum.GetValues<ResourceKind>().Where(kind => holder.AmountOf(kind) < cost.AmountOf(kind)))
        {
            var source = Gather.NearestSource(state, villagers[1].Position.Cell, kind);
            match.Enqueue(new GatherCommand(player, villagers.Select(villager => villager.Id).ToList(), source.Id));
            TestMatches.TickUntil(match, () => holder.AmountOf(kind) >= cost.AmountOf(kind));
        }

        Site.Halt(match, villagers);
    }

    /// <summary>
    /// Gathers what the Player's next Age costs, orders the Age Advance at its Town Center and
    /// ticks until the Player is in that Age.
    /// </summary>
    public static void ToNextAge(Match match, PlayerId player)
    {
        var holder = match.State.Players[player.Value - 1];
        var age = holder.Age;
        Afford(match, player, holder.NextAge!.AdvanceCost);
        match.Enqueue(new AgeAdvanceCommand(player, Train.TownCenter(match, player).Id));
        TestMatches.TickUntil(match, () => holder.Age > age);
    }
}
