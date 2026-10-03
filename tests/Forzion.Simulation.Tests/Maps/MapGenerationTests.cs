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

    private static Match TwoPlayerMatch(int width, int height, ulong seed) =>
        Match.Create(new MatchConfig(
            seed,
            new MapConfig(width, height),
            [new PlayerConfig(TestMatches.FirstFaction), new PlayerConfig(TestMatches.FirstFaction)]));
}
