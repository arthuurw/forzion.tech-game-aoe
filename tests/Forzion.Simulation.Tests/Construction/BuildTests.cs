using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Construction;

public class BuildTests
{
    [Fact]
    public void A_Villager_walks_up_to_a_House_it_was_sent_to_build_and_completes_it_after_its_build_time_of_work()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var house = PlaceHouse(match, [villager.Id]);

        Assert.Equal(house.Id, villager.ConstructionSite);

        var worked = 0;

        for (var tick = 0; !house.IsComplete; tick++)
        {
            Assert.True(tick < Gather.TickLimit);
            match.Tick();

            // Work only counts while the Villager stands beside the site.
            var working = !villager.IsMoving && Gather.Touches(house, villager.Position.Cell);
            worked += working ? 1 : 0;
            Assert.Equal(Math.Min(worked, house.BuildTime), house.BuildProgress);
        }

        Assert.Equal(house.BuildTime, worked);
        Assert.Equal([new BuildingCompleted(house.Id)], match.Events);
        Assert.Null(villager.ConstructionSite);

        match.Tick();

        Assert.False(villager.IsMoving);
        Assert.Equal(house.BuildTime, house.BuildProgress);
    }

    [Fact]
    public void A_construction_site_no_Villager_builds_makes_no_progress()
    {
        var match = TestMatches.TwoPlayerMatch();
        var house = PlaceHouse(match, []);

        for (var tick = 0; tick < 2 * house.BuildTime; tick++)
        {
            match.Tick();
        }

        Assert.Equal(0, house.BuildProgress);
        Assert.False(house.IsComplete);
    }

    /// <summary>
    /// Gathers the Wood for a House with the first Player's Villagers, places it near the Town
    /// Center with the given builders and ticks once to apply the placement.
    /// </summary>
    private static BuildingState PlaceHouse(Match match, IReadOnlyList<EntityId> builders)
    {
        Site.Stockpile(match, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        var origin = Site.FreeOriginNear(match.State, match.State.Buildings[0].Origin, Match.BuildingSize(BuildingKind.House));
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, builders));
        match.Tick();

        return match.State.Buildings.Single(building => building.Kind == BuildingKind.House);
    }
}
