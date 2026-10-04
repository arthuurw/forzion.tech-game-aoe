using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Maps;
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
        var house = MapProbe.Square(origin, Match.BuildingSize(BuildingKind.House)).ToList();

        Assert.Contains(walker.Path, house.Contains);

        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []));
        var visited = Walk.UntilStopped(match, walker);

        Assert.Equal(CellKind.Building, match.State.Map[origin]);
        Assert.Equal(MapPosition.CentreOf(destination), walker.Position);
        Assert.DoesNotContain(visited, house.Contains);
    }

    [Fact]
    public void A_Villager_carrying_its_load_whose_way_new_buildings_block_still_delivers_it_and_goes_back_to_its_source()
    {
        var match = TestMatches.TwoPlayerMatch();
        var state = match.State;
        var player = state.Players[0];
        var townCenter = state.Buildings[0];
        Site.Stockpile(match, TestMatches.FirstPlayer, 3 * Match.BuildingCost(BuildingKind.House).Wood);
        var carrier = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var source = Gather.NearestSource(state, AwayFrom(townCenter), ResourceKind.Wood);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [carrier.Id], source.Id));
        Gather.Until(match, () => carrier.GatherPhase == GatherPhase.ToDropOffPoint);

        PlaceHouses(match, HousesHemmingIn(match, carrier, cell => MapProbe.IsBeside(townCenter, cell)));
        var wood = player.AmountOf(ResourceKind.Wood);
        var load = carrier.Load.Amount;
        Gather.Until(match, () => carrier.GatherPhase != GatherPhase.ToDropOffPoint);

        Assert.Equal(wood + load, player.AmountOf(ResourceKind.Wood));
        Assert.Equal(GatherPhase.ToSource, carrier.GatherPhase);
        Assert.Equal(source.Id, carrier.GatherSource);
    }

    [Fact]
    public void A_Villager_walking_to_its_source_whose_way_new_buildings_block_still_walks_up_to_it_and_gathers()
    {
        var match = TestMatches.TwoPlayerMatch();
        var state = match.State;
        Site.Stockpile(match, TestMatches.FirstPlayer, 3 * Match.BuildingCost(BuildingKind.House).Wood);
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var source = Gather.NearestSource(state, AwayFrom(state.Buildings[0]), ResourceKind.Wood);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        match.Tick();

        PlaceHouses(match, HousesHemmingIn(match, villager, cell => MapProbe.Touch(cell, source.Cell)));
        Gather.Until(match, () => villager.GatherPhase != GatherPhase.ToSource);

        Assert.Equal(GatherPhase.Gathering, villager.GatherPhase);
        Assert.True(MapProbe.Touch(villager.Position.Cell, source.Cell));
    }

    [Fact]
    public void A_Villager_walking_to_a_site_whose_way_new_buildings_block_still_walks_up_to_it_and_builds_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var state = match.State;
        Site.Stockpile(match, TestMatches.FirstPlayer, 4 * Match.BuildingCost(BuildingKind.House).Wood);
        var builder = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var size = Match.BuildingSize(BuildingKind.House);

        // Two rings of free Cells around the site leave room for Houses all around it.
        var ringed = Site.FreeOriginNear(state, AwayFrom(state.Buildings[0]), size + 2);
        var origin = new CellPosition(ringed.X + 1, ringed.Y + 1);
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, [builder.Id]));
        match.Tick();
        var site = state.Buildings[^1];

        PlaceHouses(match, HousesHemmingIn(match, builder, cell => MapProbe.IsBeside(site, cell)));
        Walk.UntilStopped(match, builder);
        match.Tick();

        Assert.Equal(site.Id, builder.ConstructionSite);
        Assert.True(MapProbe.IsBeside(site, builder.Position.Cell));
        Assert.True(site.BuildProgress > 0);
    }

    /// <summary>A Cell some way out from the side of the building with the highest X.</summary>
    private static CellPosition AwayFrom(BuildingState building) =>
        new(building.Origin.X + building.Width + 5, building.Origin.Y + (building.Height / 2));

    /// <summary>Places the Player's Houses on the origins, in order and with no builder, and ticks once to apply them.</summary>
    private static void PlaceHouses(Match match, IEnumerable<CellPosition> origins)
    {
        foreach (var origin in origins)
        {
            match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []));
        }

        match.Tick();
    }

    /// <summary>
    /// The origins of up to three Houses that can be placed now, one of them over the last Cell
    /// of the unit's path, and that leave every Cell nearest to that Cell in a straight line,
    /// among those the unit can then walk to, away from the Cells <paramref name="isWanted"/>
    /// holds for, while the unit can still walk to one of those. A unit sent on a new way to
    /// the Cell nearest its old destination would stop away from where its job takes it.
    /// </summary>
    private static List<CellPosition> HousesHemmingIn(Match match, UnitState unit, Func<CellPosition, bool> isWanted)
    {
        var size = Match.BuildingSize(BuildingKind.House);
        var destination = unit.Path[^1];
        var candidates = MapProbe.Square(new CellPosition(destination.X - 3, destination.Y - 3), 6)
            .Where(origin => match.CanPlace(BuildingKind.House, origin))
            .ToList();

        foreach (var houses in Combinations(candidates, 3))
        {
            var blocked = houses.SelectMany(origin => MapProbe.Square(origin, size)).ToList();

            if (!blocked.Contains(destination) || blocked.Distinct().Count() < blocked.Count)
            {
                continue;
            }

            var reachable = MapProbe.ReachableFrom(match.State.Map, unit.Position.Cell, blocked.ToHashSet());
            var nearest = reachable.Min(cell => Walk.SquaredDistance(cell, destination));

            if (reachable.Any(isWanted)
                && !reachable.Any(cell => Walk.SquaredDistance(cell, destination) == nearest && isWanted(cell)))
            {
                return houses;
            }
        }

        throw new InvalidOperationException("No Houses hem the unit in.");
    }

    /// <summary>Every choice of one up to <paramref name="most"/> of the items, fewest first, each keeping the items' order.</summary>
    private static IEnumerable<List<T>> Combinations<T>(IReadOnlyList<T> items, int most)
    {
        IEnumerable<List<T>> From(int start, int count) => count == 0
            ? [[]]
            : Enumerable.Range(start, items.Count - start)
                .SelectMany(index => From(index + 1, count - 1).Select(rest => rest.Prepend(items[index]).ToList()));

        return Enumerable.Range(1, most).SelectMany(count => From(0, count));
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
            foreach (var origin in MapProbe.Square(new CellPosition(path[index].X - size + 1, path[index].Y - size + 1), size))
            {
                if (match.CanPlace(BuildingKind.House, origin) && !MapProbe.Square(origin, size).Contains(path[^1]))
                {
                    return origin;
                }
            }
        }

        throw new InvalidOperationException("No House fits across the path.");
    }
}
