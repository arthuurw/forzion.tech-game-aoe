using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

/// <summary>Helpers of the economy tests, built on the simulation's public interface only.</summary>
internal static class Gather
{
    /// <summary>The source of the given Resource nearest to the Cell; between sources equally near, the one with the lowest ID.</summary>
    public static ResourceSourceState NearestSource(MatchState state, CellPosition cell, ResourceKind kind) =>
        state.ResourceSources
            .Where(source => source.Kind == kind)
            .OrderBy(source => Walk.SquaredDistance(source.Cell, cell))
            .ThenBy(source => source.Id.Value)
            .First();

    /// <summary>The source with the given ID, or null once it has been depleted.</summary>
    public static ResourceSourceState? FindSource(MatchState state, EntityId id) =>
        state.ResourceSources.SingleOrDefault(source => source.Id == id);

    /// <summary>
    /// Sends the first Player's middle Villager to gather the Food nearest to it and ticks the
    /// match until the Villager carries a full load and sets out to deliver it.
    /// </summary>
    public static (UnitState Villager, ResourceSourceState Source) UntilFirstFullLoad(Match match)
    {
        var villager = TestMatches.MiddleVillager(match);
        var source = NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.GatherPhase == GatherPhase.ToDropOffPoint);

        return (villager, source);
    }

    /// <summary>
    /// Ticks the match until the Villager's Player holds more Food than it did when called and
    /// returns the largest load the Villager carried on the way, its load when called included.
    /// </summary>
    public static int LargestLoadUntilDelivery(Match match, UnitState villager)
    {
        var player = match.State.Players.First(player => player.Id == villager.Owner);
        var held = player.AmountOf(ResourceKind.Food);
        var largest = villager.Load.Amount;

        TestMatches.TickUntil(match, () =>
        {
            largest = Math.Max(largest, villager.Load.Amount);

            return player.AmountOf(ResourceKind.Food) > held;
        });

        return largest;
    }
}
