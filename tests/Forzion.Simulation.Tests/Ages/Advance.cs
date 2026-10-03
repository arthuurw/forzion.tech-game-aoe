using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
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
            Gather.Until(match, () => holder.AmountOf(kind) >= cost.AmountOf(kind));
        }

        Site.Halt(match, villagers);
    }

    /// <summary>The next Age of the Player's Faction, which an Age Advance would take it to.</summary>
    public static FactionAge NextAge(PlayerState player) => player.Faction.Ages[player.Age];

    /// <summary>
    /// Gathers what the Player's next Age costs, orders the Age Advance at its Town Center and
    /// ticks until the Player is in that Age.
    /// </summary>
    public static void ToNextAge(Match match, PlayerId player)
    {
        var holder = match.State.Players[player.Value - 1];
        var age = holder.Age;
        Afford(match, player, NextAge(holder).AdvanceCost);
        match.Enqueue(new AgeAdvanceCommand(player, Train.TownCenter(match, player).Id));
        Gather.Until(match, () => holder.Age > age);
    }

    /// <summary>
    /// Prepares two matches alike, made by <paramref name="create"/>, with
    /// <paramref name="prepare"/>, sends the command it returns in one of them only, and checks
    /// that it was rejected for <paramref name="reason"/> and left the two matches with the same hash.
    /// </summary>
    public static void AssertRejected(RejectionReason reason, Func<Match> create, Func<Match, Command> prepare)
    {
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
