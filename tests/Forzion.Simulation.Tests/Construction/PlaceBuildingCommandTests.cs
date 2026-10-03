using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

public class PlaceBuildingCommandTests
{
    [Fact]
    public void Placing_a_House_on_free_Cells_pays_its_cost_and_leaves_an_unfinished_House_on_them()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var cost = Match.BuildingCost(BuildingKind.House);
        Site.Stockpile(match, TestMatches.FirstPlayer, cost.Wood);
        var before = Enum.GetValues<ResourceKind>().Select(player.AmountOf).ToList();
        var origin = Site.FreeOriginNear(match.State, match.State.Buildings[0].Origin, Match.BuildingSize(BuildingKind.House));
        Assert.True(match.CanPlace(BuildingKind.House, origin));
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []));

        match.Tick();

        var house = match.State.Buildings.Single(building => building.Kind == BuildingKind.House);
        Assert.Equal(TestMatches.FirstPlayer, house.Owner);
        Assert.Equal(origin, house.Origin);
        Assert.Equal(Match.BuildingSize(BuildingKind.House), house.Width);
        Assert.Equal(Match.BuildingSize(BuildingKind.House), house.Height);
        Assert.False(house.IsComplete);
        Assert.Equal(0, house.BuildProgress);
        Assert.All(MapProbe.Footprint(house), cell => Assert.Equal(CellKind.Building, match.State.Map[cell]));
        Assert.Equal(
            [before[0] - cost.Food, before[1] - cost.Wood, before[2] - cost.Gold],
            Enum.GetValues<ResourceKind>().Select(player.AmountOf));
        Assert.True(cost.Wood > 0);
    }

    [Fact]
    public void Placing_a_building_the_Player_cannot_afford_is_rejected_and_changes_nothing()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var cost = Match.BuildingCost(BuildingKind.House);
        Site.Stockpile(withRejection, TestMatches.FirstPlayer, cost.Wood);
        Site.Stockpile(without, TestMatches.FirstPlayer, cost.Wood);
        var origin = Site.FreeOriginNear(withRejection.State, withRejection.State.Buildings[0].Origin, Match.BuildingSize(BuildingKind.Barracks));
        var command = new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.Barracks, origin, []);
        withRejection.Enqueue(command);

        withRejection.Tick();
        without.Tick();

        Assert.True(withRejection.State.Players[0].AmountOf(ResourceKind.Wood) < Match.BuildingCost(BuildingKind.Barracks).Wood);
        Assert.Equal([new CommandRejected(command, RejectionReason.NotEnoughResources)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
        Assert.Equal(2, withRejection.State.Buildings.Count);
    }

    [Theory]
    [InlineData(BuildingKind.TownCenter)]
    [InlineData((BuildingKind)99)]
    public void Placing_a_kind_of_building_Players_do_not_build_is_rejected_and_changes_nothing(BuildingKind kind)
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var origin = Site.FreeOriginNear(withRejection.State, withRejection.State.Buildings[0].Origin, 3);
        var command = new PlaceBuildingCommand(TestMatches.FirstPlayer, kind, origin, []);
        withRejection.Enqueue(command);

        withRejection.Tick();
        without.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.BuildingNotPlaceable)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void A_builder_that_no_longer_exists_is_skipped_and_the_other_builders_set_out()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[0];
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, [new EntityId(100_000), villager.Id]);

        Assert.Empty(match.Events);
        Assert.Equal(BuildingKind.House, house.Kind);
        Assert.Equal(house.Id, villager.ConstructionSite);
    }

    [Fact]
    public void Placing_a_building_with_builders_none_of_which_exist_is_rejected_and_changes_nothing()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        Site.Stockpile(withRejection, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        Site.Stockpile(without, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        var origin = Site.FreeOriginNear(withRejection.State, withRejection.State.Buildings[0].Origin, Match.BuildingSize(BuildingKind.House));
        var command = new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, [new EntityId(100_000)]);
        withRejection.Enqueue(command);

        withRejection.Tick();
        without.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnknownUnit)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void Placing_a_building_with_another_Players_Villager_as_builder_is_rejected_and_changes_nothing()
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var command = AffordableHouseWith(
            withRejection, without, withRejection.State.Units.First(unit => unit.Owner == TestMatches.SecondPlayer).Id);

        withRejection.Tick();
        without.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.UnitOfAnotherPlayer)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    /// <summary>
    /// Gives the first Player of both matches the Wood for a House and enqueues, in
    /// <paramref name="withRejection"/> only, a valid placement whose builders are its first
    /// Villager and <paramref name="other"/>.
    /// </summary>
    private static PlaceBuildingCommand AffordableHouseWith(Match withRejection, Match without, EntityId other)
    {
        Site.Stockpile(withRejection, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        Site.Stockpile(without, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        var villager = Site.VillagersOf(withRejection, TestMatches.FirstPlayer)[0];
        var origin = Site.FreeOriginNear(withRejection.State, withRejection.State.Buildings[0].Origin, Match.BuildingSize(BuildingKind.House));
        var command = new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, [villager.Id, other]);
        withRejection.Enqueue(command);

        return command;
    }

    /// <summary>Where a House cannot go: the reason is in the name, the origin is found in the match.</summary>
    public static TheoryData<string> InvalidSpots() => new() { "past_the_edge_of_the_map", "over_the_Town_Center", "over_a_resource_source", "under_a_Villager" };

    [Theory]
    [MemberData(nameof(InvalidSpots))]
    public void Placing_a_building_where_its_Cells_are_not_all_free_is_rejected_and_changes_nothing(string spot)
    {
        var withRejection = TestMatches.TwoPlayerMatch();
        var without = TestMatches.TwoPlayerMatch();
        var origin = InvalidOrigin(withRejection, spot);
        InvalidOrigin(without, spot);
        var command = new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, []);
        withRejection.Enqueue(command);

        Assert.False(withRejection.CanPlace(BuildingKind.House, origin));

        withRejection.Tick();
        without.Tick();

        Assert.Equal([new CommandRejected(command, RejectionReason.InvalidPlacement)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
        Assert.Equal(2, withRejection.State.Buildings.Count);
    }

    /// <summary>
    /// Gives the first Player enough Wood for a House, then returns an origin where a House
    /// cannot go for the reason <paramref name="spot"/> names.
    /// </summary>
    private static CellPosition InvalidOrigin(Match match, string spot)
    {
        var state = match.State;
        var size = Match.BuildingSize(BuildingKind.House);
        Site.Stockpile(match, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);

        switch (spot)
        {
            case "past_the_edge_of_the_map":
                return new CellPosition(state.Map.Width - size + 1, state.Map.Height / 2);
            case "over_the_Town_Center":
                var townCenter = state.Buildings[0];

                return new CellPosition(townCenter.Origin.X + townCenter.Width - 1, townCenter.Origin.Y + townCenter.Height - 1);
            case "over_a_resource_source":
                return state.ResourceSources[0].Cell;
            case "under_a_Villager":
                var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[0];
                var origin = Site.FreeOriginNear(state, state.Buildings[0].Origin, size);
                match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], new CellPosition(origin.X + 1, origin.Y + 1)));
                Walk.UntilStopped(match, villager);

                return origin;
            default:
                throw new ArgumentOutOfRangeException(nameof(spot), spot, null);
        }
    }
}
