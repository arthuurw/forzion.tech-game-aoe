using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

public class OrderSwitchTests
{
    [Fact]
    public void A_building_Villager_ordered_to_move_stops_building_even_beside_the_same_site()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var house = BuildingFor(match, villager, ticks: 20);

        // The far side of the site: the move ends beside it again.
        var destination = Site.Square(new CellPosition(house.Origin.X - 1, house.Origin.Y - 1), house.Width + 2)
            .Where(cell => Gather.Touches(house, cell))
            .OrderByDescending(cell => Walk.SquaredDistance(cell, villager.Position.Cell))
            .First();

        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], destination));
        match.Tick();
        var progress = house.BuildProgress;
        Walk.UntilStopped(match, villager);

        Assert.Null(villager.ConstructionSite);
        Assert.Equal(MapPosition.CentreOf(destination), villager.Position);
        Assert.Equal(progress, house.BuildProgress);
    }

    [Fact]
    public void A_building_Villager_ordered_to_gather_stops_building_and_gathers()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var house = BuildingFor(match, villager, ticks: 20);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);

        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        match.Tick();
        var progress = house.BuildProgress;
        Gather.Until(match, () => villager.Load.Amount > 0);

        Assert.Null(villager.ConstructionSite);
        Assert.Equal(ResourceKind.Food, villager.Load.Resource);
        Assert.Equal(progress, house.BuildProgress);
    }

    [Fact]
    public void A_gathering_Villager_sent_to_build_stops_gathering_and_keeps_its_load()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(TestMatches.FirstPlayer, [villager.Id], source.Id));
        Gather.Until(match, () => villager.Load.Amount > 1);
        var load = villager.Load;

        match.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [villager.Id], house.Id));
        Gather.Until(match, () => house.IsComplete);

        Assert.Equal(GatherPhase.None, villager.GatherPhase);
        Assert.Null(villager.GatherSource);
        Assert.Equal(load, villager.Load);
    }

    /// <summary>
    /// Places a House of the first Player with <paramref name="villager"/> as its builder and
    /// ticks until the Villager has worked on it for the given number of ticks.
    /// </summary>
    private static BuildingState BuildingFor(Match match, UnitState villager, int ticks)
    {
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, [villager.Id]);
        Gather.Until(match, () => house.BuildProgress == ticks);

        return house;
    }
}
