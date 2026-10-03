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
    public void Villagers_building_the_same_site_add_their_work_and_complete_it_sooner()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villagers = Site.VillagersOf(match, TestMatches.FirstPlayer);
        var house = PlaceHouse(match, villagers.Select(villager => villager.Id).ToList());
        var ticks = 0;
        var mostAtOnce = 0;

        while (!house.IsComplete)
        {
            Assert.True(ticks < Gather.TickLimit);
            var before = house.BuildProgress;
            match.Tick();
            ticks++;

            // Each Villager standing beside the site adds one tick of work.
            var working = villagers.Count(villager => !villager.IsMoving && Gather.Touches(house, villager.Position.Cell));
            mostAtOnce = Math.Max(mostAtOnce, working);
            Assert.Equal(Math.Min(before + working, house.BuildTime), house.BuildProgress);
        }

        Assert.Equal(villagers.Count, mostAtOnce);
        Assert.True(ticks < house.BuildTime / 2);
        Assert.All(villagers, villager => Assert.Null(villager.ConstructionSite));
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
