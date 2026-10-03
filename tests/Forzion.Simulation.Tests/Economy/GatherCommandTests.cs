using Forzion.Simulation.Tests.Maps;
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

        Gather.Until(match, () => villager.Load.Amount > 0);

        Assert.True(Gather.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(ResourceKind.Food, villager.Load.Resource);
        Assert.Equal(before - villager.Load.Amount, source.Amount);
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
            load = delivered ? load : villager.Load.Amount;

            return delivered;
        });

        // The first delivery: a full load, handed over beside the Town Center.
        Assert.True(load > 1);
        Assert.Equal(load, player.AmountOf(ResourceKind.Food));
        Assert.Equal(0, villager.Load.Amount);
        Assert.True(Gather.Touches(townCenter, villager.Position.Cell));

        // With no further command, back to the source for more.
        Gather.Until(match, () => villager.Load.Amount > 0);

        Assert.True(Gather.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(initial, source.Amount + villager.Load.Amount + player.AmountOf(ResourceKind.Food));

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

        Gather.Until(match, () => villager.GatherPhase == GatherPhase.ToDropOffPoint);

        // Walking away to deliver: neither the load nor the source changes on the way.
        var load = villager.Load.Amount;
        var left = source.Amount;

        for (var tick = 0; villager.GatherPhase == GatherPhase.ToDropOffPoint; tick++)
        {
            Assert.True(tick < Gather.TickLimit);
            Assert.Equal(load, villager.Load.Amount);
            Assert.Equal(left, source.Amount);

            match.Tick();
        }

        Assert.True(load > 1);
    }

    [Fact]
    public void A_gather_from_a_source_that_does_not_exist_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var command = new GatherCommand(TestMatches.FirstPlayer, [villager.Id], match.State.Buildings[0].Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownResourceSource)], match.Events);
        Assert.Equal(GatherPhase.None, villager.GatherPhase);
        Assert.False(villager.IsMoving);
    }

    [Fact]
    public void A_unit_that_no_longer_exists_is_skipped_and_the_other_units_of_the_gather_set_out()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Wood);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [new EntityId(100_000), villager.Id], source.Id));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(source.Id, villager.GatherSource);
        Assert.Equal(GatherPhase.ToSource, villager.GatherPhase);
    }

    [Fact]
    public void A_gather_by_units_none_of_which_exist_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var source = Gather.NearestSource(match.State, Walk.MiddleVillager(match).Position.Cell, ResourceKind.Wood);
        var command = new GatherCommand(TestMatches.FirstPlayer, [new EntityId(100_000), new EntityId(100_001)], source.Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownUnit)], match.Events);
    }

    [Fact]
    public void A_gather_by_another_Players_unit_is_rejected_and_sends_none_of_its_units()
    {
        var match = TestMatches.TwoPlayerMatch();
        var own = Walk.MiddleVillager(match);
        var foreign = match.State.Units.First(unit => unit.Owner == TestMatches.SecondPlayer);
        var source = Gather.NearestSource(match.State, own.Position.Cell, ResourceKind.Wood);
        var command = new GatherCommand(TestMatches.FirstPlayer, [own.Id, foreign.Id], source.Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnitOfAnotherPlayer)], match.Events);
        Assert.Equal(GatherPhase.None, own.GatherPhase);
        Assert.Equal(GatherPhase.None, foreign.GatherPhase);
    }

    [Fact]
    public void A_rejected_gather_leaves_the_state_as_if_it_had_not_been_sent()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(withRejection);
        var foreign = withRejection.State.UnitsOf(TestMatches.SecondPlayer).First();
        var source = Gather.NearestSource(withRejection.State, villager.Position.Cell, ResourceKind.Gold);
        withRejection.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id, foreign.Id], source.Id));

        withRejection.Tick();
        without.Tick();

        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void A_gathering_Villager_ordered_to_move_stops_gathering_and_keeps_its_load()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var destination = Walk.BehindTownCenter(match);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        Gather.Until(match, () => villager.Load.Amount > 1);
        var load = villager.Load.Amount;
        var left = source.Amount;

        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], destination));
        Walk.UntilStopped(match, villager);

        for (var tick = 0; tick < 200; tick++)
        {
            match.Tick();
        }

        Assert.Equal(MapPosition.CentreOf(destination), villager.Position);
        Assert.Equal(GatherPhase.None, villager.GatherPhase);
        Assert.Null(villager.GatherSource);
        Assert.Equal(load, villager.Load.Amount);
        Assert.Equal(left, source.Amount);
    }

    [Fact]
    public void A_Villager_that_starts_on_another_Resource_drops_the_load_it_carried()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var villager = Walk.MiddleVillager(match);
        var food = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var wood = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Wood);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], food.Id));
        Gather.Until(match, () => villager.Load.Amount > 1);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], wood.Id));
        Gather.Until(match, () => villager.Load.Resource == ResourceKind.Wood);

        Assert.Equal(new Load(ResourceKind.Wood, 1), villager.Load);
        Assert.Equal(0, player.AmountOf(ResourceKind.Food));
    }
}
