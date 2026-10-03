using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Maps;

public class StartingEntitiesTests
{
    [Fact]
    public void Each_Player_starts_with_one_Town_Center()
    {
        var state = TestMatches.TwoPlayerMatch().State;

        Assert.Collection(
            state.Buildings,
            first =>
            {
                Assert.Equal(BuildingKind.TownCenter, first.Kind);
                Assert.Equal(TestMatches.FirstPlayer, first.Owner);
            },
            second =>
            {
                Assert.Equal(BuildingKind.TownCenter, second.Kind);
                Assert.Equal(TestMatches.SecondPlayer, second.Owner);
            });
    }

    [Fact]
    public void A_Town_Center_occupies_a_rectangle_of_Cells_inside_the_map()
    {
        var state = TestMatches.TwoPlayerMatch().State;

        Assert.All(state.Buildings, building =>
        {
            Assert.True(building.Width > 0);
            Assert.True(building.Height > 0);
            Assert.All(MapProbe.Footprint(building), cell => Assert.Equal(CellKind.Building, state.Map[cell]));
        });
    }

    [Fact]
    public void Only_the_Cells_under_a_building_are_occupied_by_a_building()
    {
        var state = TestMatches.TwoPlayerMatch().State;

        var occupied = MapProbe.AllCells(state.Map).Count(cell => state.Map[cell] == CellKind.Building);

        Assert.Equal(state.Buildings.Sum(building => building.Width * building.Height), occupied);
    }

    [Fact]
    public void The_Town_Centers_of_the_two_Players_mirror_each_other()
    {
        var state = TestMatches.TwoPlayerMatch().State;
        var first = state.Buildings[0];
        var second = state.Buildings[1];

        Assert.Equal(
            MapProbe.Footprint(first).Select(cell => MapProbe.Mirror(state.Map, cell)).ToHashSet(),
            MapProbe.Footprint(second).ToHashSet());
    }

    [Fact]
    public void Reading_a_Cell_outside_the_map_is_an_error()
    {
        var map = TestMatches.TwoPlayerMatch().State.Map;

        Assert.False(map.Contains(new CellPosition(map.Width, 0)));
        Assert.False(map.Contains(new CellPosition(0, -1)));
        Assert.True(map.Contains(new CellPosition(map.Width - 1, map.Height - 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => map[new CellPosition(map.Width, 0)]);
    }
}
