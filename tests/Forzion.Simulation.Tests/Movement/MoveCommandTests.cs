using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Movement;

public class MoveCommandTests
{
    [Fact]
    public void A_Villager_sent_behind_the_Town_Center_walks_around_it_and_stops_on_the_destination()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var destination = Walk.BehindTownCenter(match);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], destination));

        var visited = Walk.UntilStopped(match, villager);

        Assert.Equal(MapPosition.CentreOf(destination), villager.Position);
        Assert.All(visited, cell => Assert.Equal(CellKind.Free, match.State.Map[cell]));

        // The Villager and the destination face each other across the 3 by 3 Town Center. No
        // way around is shorter than two Cells sideways, four along the building and two
        // back: eight steps, nine Cells with the one it set out from.
        Assert.Equal(9, visited.Count);
    }

    [Fact]
    public void A_Villager_sent_onto_the_enemy_Town_Center_stops_beside_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var enemyTownCenter = match.State.Buildings[1];
        var destination = new CellPosition(enemyTownCenter.Origin.X + 1, enemyTownCenter.Origin.Y + 1);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], destination));

        Walk.UntilStopped(match, villager);

        // The middle Cell of the 3 by 3 building: the Cells nearest to it that are not under
        // the building are the four two Cells away in a straight line.
        var stopped = villager.Position.Cell;
        Assert.Equal(MapPosition.CentreOf(stopped), villager.Position);
        Assert.Equal(CellKind.Free, match.State.Map[stopped]);
        Assert.Equal(2, Math.Abs(stopped.X - destination.X) + Math.Abs(stopped.Y - destination.Y));
        Assert.True(stopped.X == destination.X || stopped.Y == destination.Y);
    }

    /// <summary>Seeds crossed with destinations spread evenly over the map, whatever their Cells hold.</summary>
    public static TheoryData<ulong, int, int> SeedsAndDestinations()
    {
        var data = new TheoryData<ulong, int, int>();

        for (var seed = 1UL; seed <= 12; seed++)
        {
            for (var y = 1; y < 48; y += 9)
            {
                for (var x = (int)seed % 7; x < 64; x += 7)
                {
                    data.Add(seed, x, y);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SeedsAndDestinations))]
    public void A_Villager_stops_on_the_reachable_Cell_nearest_to_the_destination(ulong seed, int x, int y)
    {
        var match = TestMatches.TwoPlayerMatch(seed);
        var map = match.State.Map;
        var villager = Walk.MiddleVillager(match);
        var destination = new CellPosition(x, y);
        var reachable = MapProbe.ReachableFrom(map, villager.Position.Cell);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], destination));

        var visited = Walk.UntilStopped(match, villager);

        // Zero when the destination itself can be reached.
        var nearest = reachable.Min(cell => Walk.SquaredDistance(cell, destination));
        Assert.Equal(MapPosition.CentreOf(villager.Position.Cell), villager.Position);
        Assert.Equal(nearest, Walk.SquaredDistance(villager.Position.Cell, destination));
        Assert.All(visited, cell => Assert.Equal(CellKind.Free, map[cell]));
    }

    [Fact]
    public void Several_units_moved_by_one_command_all_reach_the_destination()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villagers = match.State.Units.Where(unit => unit.Owner == TestMatches.FirstPlayer).ToList();
        var destination = Walk.BehindTownCenter(match);
        match.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), destination));

        foreach (var villager in villagers)
        {
            Walk.UntilStopped(match, villager);
        }

        Assert.Equal(3, villagers.Count);
        Assert.All(villagers, villager => Assert.Equal(MapPosition.CentreOf(destination), villager.Position));
    }

    [Fact]
    public void A_unit_that_was_not_ordered_to_move_stays_where_it_is()
    {
        var match = TestMatches.TwoPlayerMatch();
        var bystander = match.State.Units[0];
        var before = bystander.Position;
        match.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer, [Walk.MiddleVillager(match).Id], Walk.BehindTownCenter(match)));

        Walk.UntilStopped(match, Walk.MiddleVillager(match));

        Assert.Equal(before, bystander.Position);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(5, -1)]
    [InlineData(64, 5)]
    [InlineData(5, 48)]
    public void A_move_to_a_Cell_outside_the_map_is_rejected(int x, int y)
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var command = new MoveCommand(TestMatches.FirstPlayer, [villager.Id], new CellPosition(x, y));
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.DestinationOutsideMap)], match.Events);
        Assert.False(villager.IsMoving);
    }

    [Fact]
    public void A_move_of_a_unit_that_does_not_exist_is_rejected_and_moves_none_of_its_units()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var command = new MoveCommand(
            TestMatches.FirstPlayer, [villager.Id, new EntityId(100_000)], Walk.BehindTownCenter(match));
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownUnit)], match.Events);
        Assert.False(villager.IsMoving);
    }

    [Fact]
    public void A_move_of_a_building_is_rejected()
    {
        var match = TestMatches.TwoPlayerMatch();
        var command = new MoveCommand(
            TestMatches.FirstPlayer, [match.State.Buildings[0].Id], Walk.BehindTownCenter(match));
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownUnit)], match.Events);
    }

    [Fact]
    public void A_move_of_another_Players_unit_is_rejected_and_moves_none_of_its_units()
    {
        var match = TestMatches.TwoPlayerMatch();
        var own = Walk.MiddleVillager(match);
        var foreign = match.State.Units.First(unit => unit.Owner == TestMatches.SecondPlayer);
        var command = new MoveCommand(TestMatches.FirstPlayer, [own.Id, foreign.Id], Walk.BehindTownCenter(match));
        match.Enqueue(command);

        match.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnitOfAnotherPlayer)], match.Events);
        Assert.False(own.IsMoving);
        Assert.False(foreign.IsMoving);
    }

    [Fact]
    public void A_rejected_move_leaves_the_state_as_if_it_had_not_been_sent()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        withRejection.Enqueue(new MoveCommand(
            TestMatches.FirstPlayer,
            [Walk.MiddleVillager(withRejection).Id, new EntityId(100_000)],
            Walk.BehindTownCenter(withRejection)));

        withRejection.Tick();
        without.Tick();

        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void A_unit_ordered_elsewhere_while_walking_goes_to_the_new_destination()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var home = villager.Position.Cell;
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], Walk.BehindTownCenter(match)));

        // Far enough to be caught between two Cell centres, on its way around the building.
        for (var tick = 0; tick < 33; tick++)
        {
            match.Tick();
        }

        Assert.True(villager.IsMoving);
        Assert.NotEqual(MapPosition.CentreOf(villager.Position.Cell), villager.Position);

        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], home));
        var visited = Walk.UntilStopped(match, villager);

        Assert.Equal(MapPosition.CentreOf(home), villager.Position);
        Assert.All(visited, cell => Assert.Equal(CellKind.Free, match.State.Map[cell]));
    }

    [Fact]
    public void A_unit_ordered_to_the_Cell_it_is_crossing_settles_on_its_centre()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], Walk.BehindTownCenter(match)));

        for (var tick = 0; tick < 33; tick++)
        {
            match.Tick();
        }

        var crossing = villager.Position.Cell;
        Assert.NotEqual(MapPosition.CentreOf(crossing), villager.Position);

        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], crossing));
        Walk.UntilStopped(match, villager);

        Assert.Equal(MapPosition.CentreOf(crossing), villager.Position);
    }

    [Fact]
    public void A_unit_ordered_to_the_Cell_it_stands_on_does_not_move()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var before = villager.Position;
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], before.Cell));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.False(villager.IsMoving);
        Assert.Equal(before, villager.Position);
    }

    [Fact]
    public void A_walking_unit_reports_the_Cells_it_still_has_to_walk_through()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Walk.MiddleVillager(match);
        var destination = Walk.BehindTownCenter(match);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], destination));

        match.Tick();

        var from = villager.Position.Cell;
        Assert.Equal(8, villager.Path.Count);
        Assert.Equal(destination, villager.Path[^1]);
        Assert.All(villager.Path, cell =>
        {
            Assert.True(MapProbe.AreNeighbours(from, cell));
            from = cell;
        });

        Walk.UntilStopped(match, villager);

        Assert.Empty(villager.Path);
    }
}
