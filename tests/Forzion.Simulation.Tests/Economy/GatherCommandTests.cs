using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

public class GatherCommandTests
{
    [Fact]
    public void A_Villager_ordered_to_gather_walks_up_to_the_source_and_takes_from_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var before = source.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        Gather.Until(match, () => villager.CarriedAmount > 0);

        Assert.True(Gather.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(ResourceKind.Food, villager.CarriedResource);
        Assert.Equal(before - villager.CarriedAmount, source.Amount);
        Assert.Equal(source.Id, villager.GatherSource);
    }
}
