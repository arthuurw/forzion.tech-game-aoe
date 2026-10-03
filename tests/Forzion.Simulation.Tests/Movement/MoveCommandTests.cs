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
}
