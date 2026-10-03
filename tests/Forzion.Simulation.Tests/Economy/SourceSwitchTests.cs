using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

public class SourceSwitchTests
{
    // A map whose only Food sources are the two each Player has beside its Town Center.
    private const ulong TwoFoodSourcesEachSeed = 4;

    [Fact]
    public void Villagers_whose_source_runs_out_move_on_to_the_nearest_source_of_the_same_Resource()
    {
        var match = TestMatches.TwoPlayerMatch(TwoFoodSourcesEachSeed);
        var villagers = OwnVillagers(match);
        var first = Gather.NearestSource(match.State, Walk.MiddleVillager(match).Position.Cell, ResourceKind.Food);
        var next = match.State.ResourceSources
            .Where(source => source.Kind == ResourceKind.Food && source != first)
            .OrderBy(source => Walk.SquaredDistance(source.Cell, first.Cell))
            .First();
        var nextInitial = next.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), first.Id));

        Gather.Until(match, () => Gather.FindSource(match.State, first.Id) is null);

        // Both of the Player's Food sources lie beside its Town Center: the next one is nearby.
        Assert.True(Walk.SquaredDistance(first.Cell, next.Cell) <= 10 * 10);
        Assert.All(villagers, villager => Assert.Equal(next.Id, villager.GatherSource));

        // With no further command, they gather from it.
        Gather.Until(match, () => next.Amount < nextInitial);

        Assert.Contains(villagers, villager =>
            villager.GatherPhase == GatherPhase.Gathering && Gather.Touch(villager.Position.Cell, next.Cell));
    }

    private static List<UnitState> OwnVillagers(Match match) =>
        match.State.Units.Where(unit => unit.Owner == TestMatches.FirstPlayer).ToList();
}
