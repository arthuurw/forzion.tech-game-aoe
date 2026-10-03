using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

public class RepathTests
{
    [Fact]
    public void A_unit_whose_way_a_new_building_blocks_finds_another_way_to_its_destination()
    {
        var match = TestMatches.TwoPlayerMatch();
        Site.Stockpile(match, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        var walker = Site.VillagersOf(match, TestMatches.FirstPlayer)[0];
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [walker.Id], new CellPosition(match.State.Map.Width / 2, match.State.Map.Height / 2)));
        match.Tick();
        var destination = walker.Path[^1];
        var origin = OriginAcross(match, walker.Path);
        var house = Site.Square(origin, Match.BuildingSize(BuildingKind.House)).ToList();

        Assert.Contains(walker.Path, house.Contains);

        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []));
        var visited = Walk.UntilStopped(match, walker);

        Assert.Equal(CellKind.Building, match.State.Map[origin]);
        Assert.Equal(MapPosition.CentreOf(destination), walker.Position);
        Assert.DoesNotContain(visited, house.Contains);
    }

    /// <summary>
    /// The origin of a House that can be placed over a Cell of the path well ahead of the
    /// unit and short of its destination, so it blocks the way the unit was going to take.
    /// </summary>
    private static CellPosition OriginAcross(Match match, IReadOnlyList<CellPosition> path)
    {
        var size = Match.BuildingSize(BuildingKind.House);

        for (var index = 4; index < path.Count - 3; index++)
        {
            foreach (var origin in Site.Square(new CellPosition(path[index].X - size + 1, path[index].Y - size + 1), size))
            {
                if (match.CanPlace(BuildingKind.House, origin) && !Site.Square(origin, size).Contains(path[^1]))
                {
                    return origin;
                }
            }
        }

        throw new InvalidOperationException("No House fits across the path.");
    }
}
