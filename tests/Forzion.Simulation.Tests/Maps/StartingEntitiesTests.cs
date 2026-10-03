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
    public void Each_Player_starts_with_the_same_number_of_Villagers()
    {
        var state = TestMatches.TwoPlayerMatch().State;

        Assert.NotEmpty(state.Units);
        Assert.All(state.Units, unit => Assert.Equal(UnitKind.Villager, unit.Kind));
        Assert.Equal(
            state.Units.Count(unit => unit.Owner == TestMatches.FirstPlayer),
            state.Units.Count(unit => unit.Owner == TestMatches.SecondPlayer));
        Assert.Equal(state.Units.Count, state.Units.Count(unit => unit.Owner == TestMatches.FirstPlayer) * 2);
    }

    [Fact]
    public void Starting_Villagers_stand_at_the_centre_of_distinct_free_Cells_beside_their_Town_Center()
    {
        var state = TestMatches.TwoPlayerMatch().State;
        var half = Fix64.One / Fix64.FromInt(2);

        Assert.Equal(state.Units.Count, state.Units.Select(unit => unit.Position.Cell).Distinct().Count());
        Assert.All(state.Units, unit =>
        {
            var cell = unit.Position.Cell;
            var townCenter = state.Buildings.Single(building => building.Owner == unit.Owner);

            Assert.Equal(CellKind.Free, state.Map[cell]);
            Assert.Equal(Fix64.FromInt(cell.X) + half, unit.Position.X);
            Assert.Equal(Fix64.FromInt(cell.Y) + half, unit.Position.Y);
            Assert.Contains(MapProbe.Footprint(townCenter), under => MapProbe.AreNeighbours(under, cell));
        });
    }

    [Fact]
    public void The_starting_Villagers_of_the_two_Players_mirror_each_other()
    {
        var state = TestMatches.TwoPlayerMatch().State;

        Assert.Equal(
            CellsOf(TestMatches.FirstPlayer).Select(cell => MapProbe.Mirror(state.Map, cell)).ToHashSet(),
            CellsOf(TestMatches.SecondPlayer).ToHashSet());

        IEnumerable<CellPosition> CellsOf(PlayerId player) =>
            state.Units.Where(unit => unit.Owner == player).Select(unit => unit.Position.Cell);
    }

    [Fact]
    public void Entities_have_distinct_ids_and_each_collection_is_in_ascending_id_order()
    {
        var state = TestMatches.TwoPlayerMatch().State;
        var buildings = state.Buildings.Select(building => building.Id.Value).ToList();
        var units = state.Units.Select(unit => unit.Id.Value).ToList();
        var sources = state.ResourceSources.Select(source => source.Id.Value).ToList();
        var all = buildings.Concat(units).Concat(sources).ToList();

        Assert.Equal(buildings.Order(), buildings);
        Assert.Equal(units.Order(), units);
        Assert.Equal(sources.Order(), sources);
        Assert.Equal(all.Count, all.Distinct().Count());
        Assert.All(all, id => Assert.True(id >= 1));
    }

    [Fact]
    public void A_match_with_a_single_Player_has_only_that_Players_entities()
    {
        var config = new MatchConfig(1, new MapConfig(64, 48), [new PlayerConfig(TestMatches.FirstFaction)]);

        var state = Match.Create(config).State;

        Assert.Single(state.Buildings);
        Assert.All(state.Units, unit => Assert.Equal(TestMatches.FirstPlayer, unit.Owner));
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
