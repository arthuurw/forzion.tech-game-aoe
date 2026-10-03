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

    [Fact]
    public void A_Villager_carries_each_full_load_to_its_Town_Center_and_goes_back_to_the_source_on_its_own()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var townCenter = match.State.Buildings[0];
        var villager = Walk.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var initial = source.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        var load = 0;
        Gather.Until(match, () =>
        {
            var delivered = player.AmountOf(ResourceKind.Food) > 0;
            load = delivered ? load : villager.CarriedAmount;

            return delivered;
        });

        // The first delivery: a full load, handed over beside the Town Center.
        Assert.True(load > 1);
        Assert.Equal(load, player.AmountOf(ResourceKind.Food));
        Assert.Equal(0, villager.CarriedAmount);
        Assert.True(Gather.Touches(townCenter, villager.Position.Cell));

        // With no further command, back to the source for more.
        Gather.Until(match, () => villager.CarriedAmount > 0);

        Assert.True(Gather.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(initial, source.Amount + villager.CarriedAmount + player.AmountOf(ResourceKind.Food));

        // And the next trip brings another load of the same size.
        Gather.Until(match, () => player.AmountOf(ResourceKind.Food) > load);

        Assert.Equal(2 * load, player.AmountOf(ResourceKind.Food));
    }

    [Fact]
    public void A_Villager_stops_taking_from_the_source_once_it_carries_a_full_load()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        Gather.Until(match, () => villager.GatherPhase == GatherPhase.ToDropOff);

        // Walking away to deliver: neither the load nor the source changes on the way.
        var load = villager.CarriedAmount;
        var left = source.Amount;

        for (var tick = 0; villager.GatherPhase == GatherPhase.ToDropOff; tick++)
        {
            Assert.True(tick < Gather.TickLimit);
            Assert.Equal(load, villager.CarriedAmount);
            Assert.Equal(left, source.Amount);

            match.Tick();
        }

        Assert.True(load > 1);
    }
}
