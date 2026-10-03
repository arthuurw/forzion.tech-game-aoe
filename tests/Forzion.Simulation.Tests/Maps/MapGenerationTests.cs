using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Maps;

public class MapGenerationTests
{
    private static readonly ResourceKind[] AllResources = [ResourceKind.Food, ResourceKind.Wood, ResourceKind.Gold];

    /// <summary>Map sizes crossed with seeds: square, wide, tall, odd-sided and the smallest allowed.</summary>
    public static TheoryData<int, int, ulong> SizesAndSeeds()
    {
        var data = new TheoryData<int, int, ulong>();

        foreach (var (width, height) in new[] { (32, 32), (64, 48), (48, 64), (33, 35), (96, 96) })
        {
            for (var seed = 1UL; seed <= 40; seed++)
            {
                data.Add(width, height, seed);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SizesAndSeeds))]
    public void Each_Player_reaches_a_source_of_each_of_the_three_Resources(int width, int height, ulong seed)
    {
        var state = TwoPlayerMatch(width, height, seed).State;

        Assert.All(state.Players, player =>
            Assert.Equal(AllResources, MapProbe.ReachableResources(state, player.Id).Order()));
    }

    [Theory]
    [MemberData(nameof(SizesAndSeeds))]
    public void Every_resource_source_on_the_map_can_be_walked_up_to_from_both_homes(int width, int height, ulong seed)
    {
        var state = TwoPlayerMatch(width, height, seed).State;
        var reached = MapProbe.ReachableFrom(state.Map, state.Units[0].Position.Cell);

        Assert.All(state.Units, unit => Assert.Contains(unit.Position.Cell, reached));
        Assert.All(state.ResourceSources, source =>
            Assert.Contains(MapProbe.NeighboursOf(state.Map, source.Cell), reached.Contains));
    }

    [Fact]
    public void Each_resource_source_holds_a_finite_amount_and_occupies_its_Cell()
    {
        var state = TestMatches.TwoPlayerMatch().State;

        Assert.All(state.ResourceSources, source =>
        {
            Assert.True(source.Amount > 0);
            Assert.Equal(CellKind.ResourceSource, state.Map[source.Cell]);
        });
        Assert.Equal(
            state.ResourceSources.Select(source => source.Cell).ToHashSet(),
            MapProbe.AllCells(state.Map).Where(cell => state.Map[cell] == CellKind.ResourceSource).ToHashSet());
        Assert.Equal(state.ResourceSources.Count, state.ResourceSources.Select(source => source.Cell).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(SizesAndSeeds))]
    public void The_resource_sources_are_symmetric_between_the_Players(int width, int height, ulong seed)
    {
        var state = TwoPlayerMatch(width, height, seed).State;
        var sources = state.ResourceSources.Select(source => (source.Cell, source.Kind, source.Amount)).ToHashSet();

        Assert.Equal(
            sources,
            sources.Select(source => (MapProbe.Mirror(state.Map, source.Cell), source.Kind, source.Amount)).ToHashSet());
    }

    [Theory]
    [MemberData(nameof(SizesAndSeeds))]
    public void The_Cells_are_symmetric_between_the_Players(int width, int height, ulong seed)
    {
        var map = TwoPlayerMatch(width, height, seed).State.Map;

        Assert.All(MapProbe.AllCells(map), cell => Assert.Equal(map[cell], map[MapProbe.Mirror(map, cell)]));
    }

    [Theory]
    [MemberData(nameof(SizesAndSeeds))]
    public void The_Players_can_reach_each_other(int width, int height, ulong seed)
    {
        var state = TwoPlayerMatch(width, height, seed).State;
        var reached = MapProbe.ReachableFrom(state.Map, state.Units[0].Position.Cell);

        Assert.All(state.Units, unit => Assert.Contains(unit.Position.Cell, reached));
    }

    [Fact]
    public void The_map_has_forest_and_water()
    {
        var map = TestMatches.TwoPlayerMatch().State.Map;
        var kinds = MapProbe.AllCells(map).Select(cell => map[cell]).ToHashSet();

        Assert.Contains(CellKind.Forest, kinds);
        Assert.Contains(CellKind.Water, kinds);
        Assert.Contains(CellKind.Free, kinds);
    }

    [Fact]
    public void The_map_has_resource_sources_away_from_the_Town_Centers()
    {
        var state = TwoPlayerMatch(96, 96, seed: 42).State;

        Assert.Contains(state.ResourceSources, source =>
            state.Buildings.All(building =>
                Math.Abs(source.Cell.X - building.Origin.X) > 12 || Math.Abs(source.Cell.Y - building.Origin.Y) > 12));
    }

    [Fact]
    public void The_same_seed_generates_the_same_map()
    {
        var first = TestMatches.TwoPlayerMatch(seed: 7).State;
        var second = TestMatches.TwoPlayerMatch(seed: 7).State;

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    [Fact]
    public void Different_seeds_generate_different_maps()
    {
        var snapshots = Enumerable.Range(1, 20)
            .Select(seed => Snapshot(TestMatches.TwoPlayerMatch((ulong)seed).State))
            .ToList();

        Assert.Equal(snapshots.Count, snapshots.Distinct().Count());
    }

    /// <summary>The whole generated map as text: one character per Cell, then the sources, buildings and units.</summary>
    private static string Snapshot(MatchState state)
    {
        var cells = string.Concat(MapProbe.AllCells(state.Map).Select(cell => (int)state.Map[cell]));
        var sources = state.ResourceSources.Select(source => $"{source.Id}{source.Kind}{source.Cell}{source.Amount}");
        var buildings = state.Buildings.Select(building => $"{building.Id}{building.Owner}{building.Kind}{building.Origin}");
        var units = state.Units.Select(unit => $"{unit.Id}{unit.Owner}{unit.Kind}{unit.Position.X.RawValue},{unit.Position.Y.RawValue}");

        return string.Join('|', sources.Concat(buildings).Concat(units).Prepend(cells));
    }

    private static Match TwoPlayerMatch(int width, int height, ulong seed) =>
        Match.Create(TestMatches.TwoPlayerConfig(seed) with { Map = new MapConfig(width, height) });
}
