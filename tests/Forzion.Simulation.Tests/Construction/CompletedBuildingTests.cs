using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Construction;

public class CompletedBuildingTests
{
    [Fact]
    public void Each_complete_House_raises_its_Players_population_limit_by_the_same_amount_and_a_site_does_not()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villagers = Site.VillagersOf(match, TestMatches.FirstPlayer).Select(villager => villager.Id).ToList();
        var initial = match.State.PopulationLimitOf(TestMatches.FirstPlayer);
        var enemy = match.State.PopulationLimitOf(TestMatches.SecondPlayer);

        var first = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, villagers);

        Assert.True(initial > 0);
        Assert.Equal(initial, match.State.PopulationLimitOf(TestMatches.FirstPlayer));

        Gather.Until(match, () => first.IsComplete);
        var withOne = match.State.PopulationLimitOf(TestMatches.FirstPlayer);
        var second = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, villagers);
        Gather.Until(match, () => second.IsComplete);

        Assert.True(withOne > initial);
        Assert.Equal(initial + (2 * (withOne - initial)), match.State.PopulationLimitOf(TestMatches.FirstPlayer));
        Assert.Equal(enemy, match.State.PopulationLimitOf(TestMatches.SecondPlayer));
    }
}
