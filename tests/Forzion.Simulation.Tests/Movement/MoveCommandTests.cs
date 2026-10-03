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
}
