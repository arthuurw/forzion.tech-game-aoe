using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

public class CompletedBuildingTests
{
    [Fact]
    public void Each_complete_House_raises_its_Players_population_limit_by_the_same_amount_and_a_site_does_not()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villagers = Site.VillagersOf(match, TestMatches.FirstPlayer).Select(villager => villager.Id).ToList();
        var initial = match.State.PopulationLimitOf(TestMatches.FirstPlayer);
        var enemy = match.State.PopulationLimitOf(TestMatches.SecondPlayer);

        var first = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, villagers);

        Assert.True(initial > 0);
        Assert.Equal(initial, match.State.PopulationLimitOf(TestMatches.FirstPlayer));

        Gather.Until(match, () => first.IsComplete);
        var withOne = match.State.PopulationLimitOf(TestMatches.FirstPlayer);
        var second = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, villagers);
        Gather.Until(match, () => second.IsComplete);

        Assert.True(withOne > initial);
        Assert.Equal(initial + (2 * (withOne - initial)), match.State.PopulationLimitOf(TestMatches.FirstPlayer));
        Assert.Equal(enemy, match.State.PopulationLimitOf(TestMatches.SecondPlayer));
    }

    [Fact]
    public void Villagers_deliver_to_a_nearer_Storehouse_once_it_is_complete_and_not_before()
    {
        var match = TestMatches.TwoPlayerMatch();
        var state = match.State;
        var player = state.Players[0];
        var townCenter = state.Buildings[0];
        var villagers = Site.VillagersOf(match, TestMatches.FirstPlayer);
        var gatherer = villagers[1];
        Site.Stockpile(match, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.Storehouse).Wood);
        var source = Gather.NearestSource(state, gatherer.Position.Cell, ResourceKind.Wood);
        var origin = Site.FreeOriginNear(state, source.Cell, Match.BuildingSize(BuildingKind.Storehouse));
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.Storehouse, origin, []));
        match.Tick();
        var storehouse = state.Buildings[^1];

        Assert.True(Distance(storehouse, source.Cell) < Distance(townCenter, source.Cell));

        // Unfinished, the Storehouse takes no delivery: the load goes to the Town Center.
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [gatherer.Id], source.Id));
        var delivery = DeliveryBy(match, player, gatherer);

        Assert.True(Gather.Touches(townCenter, delivery));

        match.Enqueue(new BuildCommand(TestMatches.FirstPlayer, villagers.Select(villager => villager.Id).ToList(), storehouse.Id));
        Gather.Until(match, () => storehouse.IsComplete);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [gatherer.Id], Gather.NearestSource(state, gatherer.Position.Cell, ResourceKind.Wood).Id));
        delivery = DeliveryBy(match, player, gatherer);

        Assert.True(Gather.Touches(storehouse, delivery));
    }

    [Fact]
    public void A_complete_Barracks_stands_on_the_map_and_units_walk_around_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villagers = Site.VillagersOf(match, TestMatches.FirstPlayer);
        var barracks = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.Barracks, villagers.Select(villager => villager.Id).ToList());
        var completed = new List<MatchEvent>();
        Gather.Until(match, () =>
        {
            completed.AddRange(match.Events.OfType<BuildingCompleted>());

            return barracks.IsComplete;
        });

        // A walk from one side of the Barracks to the other, through the middle of it in a straight line.
        var walker = villagers[0];
        var middle = barracks.Origin.Y + (barracks.Height / 2);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [walker.Id], new CellPosition(barracks.Origin.X - 1, middle)));
        Walk.UntilStopped(match, walker);
        var destination = new CellPosition(barracks.Origin.X + barracks.Width, middle);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [walker.Id], destination));
        var visited = Walk.UntilStopped(match, walker);

        Assert.Equal([new BuildingCompleted(barracks.Id)], completed);
        Assert.Equal(TestMatches.FirstPlayer, barracks.Owner);
        Assert.Contains(barracks, match.State.Buildings);
        Assert.Equal(MapPosition.CentreOf(destination), walker.Position);
        Assert.DoesNotContain(visited, cell => MapProbe.Footprint(barracks).Contains(cell));
    }

    /// <summary>Ticks until the Player receives Wood and returns the Cell the Villager handed it over from.</summary>
    private static CellPosition DeliveryBy(Match match, PlayerState player, UnitState villager)
    {
        var before = player.AmountOf(ResourceKind.Wood);
        Gather.Until(match, () => player.AmountOf(ResourceKind.Wood) > before);

        return villager.Position.Cell;
    }

    /// <summary>Square of the distance from the Cell to the nearest Cell of the building's footprint.</summary>
    private static int Distance(BuildingState building, CellPosition cell) =>
        Walk.SquaredDistance(cell, new CellPosition(
            Math.Clamp(cell.X, building.Origin.X, building.Origin.X + building.Width - 1),
            Math.Clamp(cell.Y, building.Origin.Y, building.Origin.Y + building.Height - 1)));
}
