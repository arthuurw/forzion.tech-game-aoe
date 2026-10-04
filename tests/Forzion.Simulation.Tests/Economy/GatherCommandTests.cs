using Forzion.Simulation.Tests.Construction;
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
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var before = source.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        TestMatches.TickUntil(match, () => villager.Load.Amount > 0);

        Assert.True(MapProbe.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(ResourceKind.Food, villager.Load.Resource);
        Assert.Equal(before - villager.Load.Amount, source.Amount);
        Assert.Equal(source.Id, villager.GatherSource);
    }

    [Fact]
    public void A_Villager_ordered_to_gather_walks_to_the_Cell_beside_the_source_with_the_shortest_way_to_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var map = match.State.Map;
        var villager = TestMatches.MiddleVillager(match);
        var reachable = MapProbe.ReachableFrom(map, villager.Position.Cell);

        // A source in the corner of a square of reachable Cells: from the opposite corner, the
        // Cell beside the source's corner is one diagonal step away, while the Cells beside its
        // sides, nearer to it in a straight line, are two steps away.
        var source = match.State.ResourceSources
            .OrderBy(each => Walk.SquaredDistance(each.Cell, villager.Position.Cell))
            .ThenBy(each => each.Id.Value)
            .First(each =>
                MapProbe.Square(each.Cell, 3).Where(cell => cell != each.Cell).All(reachable.Contains)
                && reachable.Contains(new CellPosition(each.Cell.X + 2, each.Cell.Y + 2)));
        var corner = new CellPosition(source.Cell.X + 1, source.Cell.Y + 1);
        var start = new CellPosition(source.Cell.X + 2, source.Cell.Y + 2);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], start));
        Walk.UntilStopped(match, villager);
        Assert.Equal(start, villager.Position.Cell);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        match.Tick();

        Assert.Equal([corner], villager.Path);
    }

    [Fact]
    public void A_Villager_ordered_to_gather_a_source_it_cannot_reach_walks_to_the_reachable_Cell_nearest_to_it_and_waits_there()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        Site.Stockpile(match, TestMatches.FirstPlayer, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var (source, origins) = Gather.SourceToBoxIn(match, villager);

        foreach (var origin in origins)
        {
            match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []));
        }

        match.Tick();
        var start = villager.Position.Cell;
        var reachable = MapProbe.ReachableFrom(match.State.Map, start);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        Walk.UntilStopped(match, villager);
        match.Tick();

        Assert.Equal(
            reachable.Min(cell => Walk.SquaredDistance(cell, source.Cell)),
            Walk.SquaredDistance(villager.Position.Cell, source.Cell));
        Assert.True(Walk.SquaredDistance(villager.Position.Cell, source.Cell) < Walk.SquaredDistance(start, source.Cell));
        Assert.Equal(GatherPhase.ToSource, villager.GatherPhase);
        Assert.Equal(source.Id, villager.GatherSource);
        Assert.False(villager.IsMoving);
    }

    [Fact]
    public void A_Villager_with_a_full_load_that_can_reach_no_drop_off_point_walks_to_the_reachable_Cell_nearest_to_one_and_waits_there()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = match.State.Buildings[0];
        var houses = Gather.HousesBoxingIn(townCenter.Origin, townCenter.Width);
        Site.Stockpile(match, TestMatches.FirstPlayer, houses.Count * Match.BuildingCost(BuildingKind.House).Wood);

        // The Villagers step well away, out of where the Houses go.
        var villagers = Site.VillagersOf(match, TestMatches.FirstPlayer);
        var away = Site.FreeOriginNear(match.State, new CellPosition(townCenter.Origin.X + 10, townCenter.Origin.Y), 1);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, villagers.Select(each => each.Id).ToList(), away));
        TestMatches.TickUntil(match, () => villagers.All(each => !each.IsMoving));

        foreach (var origin in houses)
        {
            match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []));
        }

        match.Tick();
        Assert.Empty(match.Events.OfType<CommandRejected>());
        var (villager, _) = Gather.UntilFirstFullLoad(match);
        var load = villager.Load;
        var start = villager.Position.Cell;
        var reachable = MapProbe.ReachableFrom(match.State.Map, start);
        var centre = new CellPosition(townCenter.Origin.X + (townCenter.Width / 2), townCenter.Origin.Y + (townCenter.Height / 2));

        Walk.UntilStopped(match, villager);
        match.Tick();

        Assert.Equal(
            reachable.Min(cell => Walk.SquaredDistance(cell, centre)),
            Walk.SquaredDistance(villager.Position.Cell, centre));
        Assert.True(Walk.SquaredDistance(villager.Position.Cell, centre) < Walk.SquaredDistance(start, centre));
        Assert.Equal(GatherPhase.ToDropOffPoint, villager.GatherPhase);
        Assert.Equal(load, villager.Load);
        Assert.False(villager.IsMoving);
    }

    [Fact]
    public void A_Villager_carries_each_full_load_to_its_Town_Center_and_goes_back_to_the_source_on_its_own()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var townCenter = match.State.Buildings[0];
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var initial = source.Amount;
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        var load = Gather.LargestLoadUntilDelivery(match, villager);

        // The first delivery: a full load, handed over beside the Town Center.
        Assert.True(load > 1);
        Assert.Equal(load, Gather.Delivered(player, ResourceKind.Food));
        Assert.Equal(0, villager.Load.Amount);
        Assert.True(MapProbe.IsBeside(townCenter, villager.Position.Cell));

        // With no further command, back to the source for more.
        TestMatches.TickUntil(match, () => villager.Load.Amount > 0);

        Assert.True(MapProbe.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(initial, source.Amount + villager.Load.Amount + Gather.Delivered(player, ResourceKind.Food));

        // And the next trip brings another load of the same size.
        TestMatches.TickUntil(match, () => Gather.Delivered(player, ResourceKind.Food) > load);

        Assert.Equal(2 * load, Gather.Delivered(player, ResourceKind.Food));
    }

    [Fact]
    public void A_Villager_stops_taking_from_the_source_once_it_carries_a_full_load()
    {
        var match = TestMatches.TwoPlayerMatch();

        var (villager, source) = Gather.UntilFirstFullLoad(match);

        // Walking away to deliver: neither the load nor the source changes on the way.
        var load = villager.Load.Amount;
        var left = source.Amount;

        for (var tick = 0; villager.GatherPhase == GatherPhase.ToDropOffPoint; tick++)
        {
            Assert.True(tick < TestMatches.TickLimit);
            Assert.Equal(load, villager.Load.Amount);
            Assert.Equal(left, source.Amount);

            match.Tick();
        }

        Assert.True(load > 1);
    }

    [Fact]
    public void Gather_progress_counts_the_ticks_spent_towards_the_next_unit_and_restarts_when_one_is_taken()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.GatherPhase == GatherPhase.Gathering);

        Assert.Equal(0, villager.GatherProgress);

        for (var progress = 1; villager.Load.Amount == 0; progress++)
        {
            match.Tick();

            Assert.Equal(villager.Load.Amount == 0 ? progress : 0, villager.GatherProgress);
        }
    }

    // A player clicking the source again and again must not keep the Villager from gathering.
    [Fact]
    public void A_gathering_Villager_ordered_again_to_the_same_source_keeps_its_progress()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.GatherPhase == GatherPhase.Gathering);
        TestMatches.TickUntil(match, () => villager.GatherProgress == 5);

        for (var order = 0; villager.Load.Amount == 0; order++)
        {
            Assert.True(order < 10, "The repeated order kept the Villager from taking a unit.");
            var progress = villager.GatherProgress;

            match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
            match.Tick();

            Assert.Equal(GatherPhase.Gathering, villager.GatherPhase);
            Assert.True(villager.Load.Amount > 0 || villager.GatherProgress == progress + 1);
        }
    }

    [Fact]
    public void A_gather_from_a_source_that_does_not_exist_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = TestMatches.MiddleVillager(match);
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
        var villager = TestMatches.MiddleVillager(match);
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
        var source = Gather.NearestSource(match.State, TestMatches.MiddleVillager(match).Position.Cell, ResourceKind.Wood);
        var command = new GatherCommand(TestMatches.FirstPlayer, [new EntityId(100_000), new EntityId(100_001)], source.Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownUnit)], match.Events);
    }

    [Fact]
    public void A_gather_by_another_Players_unit_is_rejected_and_sends_none_of_its_units()
    {
        var match = TestMatches.TwoPlayerMatch();
        var own = TestMatches.MiddleVillager(match);
        var foreign = match.State.UnitsOf(TestMatches.SecondPlayer).First();
        var source = Gather.NearestSource(match.State, own.Position.Cell, ResourceKind.Wood);
        var command = new GatherCommand(TestMatches.FirstPlayer, [own.Id, foreign.Id], source.Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnitOfAnotherPlayer)], match.Events);
        Assert.Equal(GatherPhase.None, own.GatherPhase);
        Assert.Equal(GatherPhase.None, foreign.GatherPhase);
    }

    [Fact]
    public void A_gather_by_Villagers_and_soldiers_sends_the_Villagers_and_leaves_the_soldiers_to_what_they_were_doing()
    {
        var match = TestArmies.MatchWithSoldier();
        var villager = TestMatches.MiddleVillager(match);
        var soldier = match.State.SoldierOf(TestMatches.FirstPlayer);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Wood);
        var destination = TestArmies.WalkAway(match, soldier);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [soldier.Id, villager.Id], source.Id));
        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(source.Id, villager.GatherSource);
        Assert.Equal(GatherPhase.ToSource, villager.GatherPhase);
        Assert.Equal(GatherPhase.None, soldier.GatherPhase);
        Assert.Null(soldier.GatherSource);
        Assert.Equal(destination, soldier.Path[^1]);
    }

    [Fact]
    public void A_gather_by_soldiers_alone_is_rejected_and_sends_none_of_them()
    {
        var match = TestArmies.MatchWithSoldier();
        var soldier = match.State.SoldierOf(TestMatches.FirstPlayer);
        var source = Gather.NearestSource(match.State, soldier.Position.Cell, ResourceKind.Wood);
        var command = new GatherCommand(TestMatches.FirstPlayer, [soldier.Id], source.Id);
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnitCannotGather)], match.Events);
        Assert.Equal(GatherPhase.None, soldier.GatherPhase);
        Assert.False(soldier.IsMoving);
    }

    [Fact]
    public void A_rejected_gather_leaves_the_state_as_if_it_had_not_been_sent()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var villager = TestMatches.MiddleVillager(withRejection);
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
        var villager = TestMatches.MiddleVillager(match);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var destination = Walk.BehindTownCenter(match);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.Load.Amount > 1);
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
        var villager = TestMatches.MiddleVillager(match);
        var food = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        var wood = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Wood);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], food.Id));
        TestMatches.TickUntil(match, () => villager.Load.Amount > 1);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], wood.Id));
        TestMatches.TickUntil(match, () => villager.Load.Resource == ResourceKind.Wood);

        Assert.Equal(new Load(ResourceKind.Wood, 1), villager.Load);
        Assert.Equal(0, Gather.Delivered(player, ResourceKind.Food));
    }

    [Fact]
    public void A_Villager_walking_to_deliver_and_ordered_to_gather_the_same_source_delivers_first()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var (villager, source) = Gather.UntilFirstFullLoad(match);
        var full = villager.Load.Amount;

        // Already on its way when the order comes.
        match.Tick();
        match.Tick();
        Assert.Equal(GatherPhase.ToDropOffPoint, villager.GatherPhase);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));

        var largest = Gather.LargestLoadUntilDelivery(match, villager);

        Assert.Equal(full, largest);
        Assert.Equal(full, Gather.Delivered(player, ResourceKind.Food));
        Assert.True(MapProbe.IsBeside(match.State.Buildings[0], villager.Position.Cell));

        // And back to the source for the next load.
        TestMatches.TickUntil(match, () => villager.Load.Amount > 0);

        Assert.True(MapProbe.Touch(villager.Position.Cell, source.Cell));
        Assert.Equal(source.Id, villager.GatherSource);
    }

    [Fact]
    public void A_Villager_moved_away_with_a_full_load_and_ordered_to_gather_again_delivers_first()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var (villager, source) = Gather.UntilFirstFullLoad(match);
        var full = villager.Load.Amount;
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], Walk.BehindTownCenter(match)));
        Walk.UntilStopped(match, villager);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        var largest = Gather.LargestLoadUntilDelivery(match, villager);

        Assert.Equal(full, largest);
        Assert.Equal(full, Gather.Delivered(player, ResourceKind.Food));
        Assert.True(MapProbe.IsBeside(match.State.Buildings[0], villager.Position.Cell));
    }

    [Fact]
    public void A_Villager_ordered_to_gather_the_Resource_it_carries_keeps_its_load_and_fills_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var initial = Gather.NearestSource(match.State, TestMatches.MiddleVillager(match).Position.Cell, ResourceKind.Food).Amount;
        var (villager, source) = Gather.UntilFirstFullLoad(match);
        var full = villager.Load.Amount;
        TestMatches.TickUntil(match, () => villager.Load.Amount == 2);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], Walk.BehindTownCenter(match)));
        Walk.UntilStopped(match, villager);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        TestMatches.TickUntil(match, () => villager.Load.Amount != 2);

        // The next unit taken adds to the load it already carried.
        Assert.Equal(new Load(ResourceKind.Food, 3), villager.Load);

        // And the load it delivers is full, not larger.
        var delivered = Gather.Delivered(player, ResourceKind.Food);
        var largest = Gather.LargestLoadUntilDelivery(match, villager);

        Assert.Equal(full, largest);
        Assert.Equal(delivered + full, Gather.Delivered(player, ResourceKind.Food));
        Assert.Equal(initial, source.Amount + villager.Load.Amount + Gather.Delivered(player, ResourceKind.Food));
    }

    // Generated maps keep every Cell beside a Town Center free and reachable, so no test
    // through the match can block the way to some of them; this covers delivery from every
    // side instead.
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-1, 0)]
    [InlineData(-1, -1)]
    [InlineData(0, -1)]
    [InlineData(1, -1)]
    public void A_Villager_with_a_full_load_hands_it_over_beside_its_Town_Center_from_any_side(int towardsX, int towardsY)
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var townCenter = match.State.Buildings[0];
        var (villager, source) = Gather.UntilFirstFullLoad(match);
        var full = villager.Load.Amount;

        // Six Cells out from the middle of the Town Center: inside the clearing around it and
        // past its resource sources.
        var start = new CellPosition(
            townCenter.Origin.X + (townCenter.Width / 2) + (6 * towardsX),
            townCenter.Origin.Y + (townCenter.Height / 2) + (6 * towardsY));
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], start));
        Walk.UntilStopped(match, villager);
        Assert.Equal(start, villager.Position.Cell);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        Gather.LargestLoadUntilDelivery(match, villager);

        Assert.Equal(full, Gather.Delivered(player, ResourceKind.Food));
        Assert.True(MapProbe.IsBeside(townCenter, villager.Position.Cell));
    }
}
