using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

public class BuildTests
{
    [Fact]
    public void A_Villager_walks_up_to_a_House_it_was_sent_to_build_and_completes_it_after_its_build_time_of_work()
    {
        var match = TestMatches.TwoPlayerMatch();
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, [villager.Id]);

        Assert.Equal(house.Id, villager.ConstructionSite);

        var worked = 0;

        for (var tick = 0; !house.IsComplete; tick++)
        {
            Assert.True(tick < TestMatches.TickLimit);
            match.Tick();

            // Work only counts while the Villager stands beside the site.
            var working = !villager.IsMoving && MapProbe.IsBeside(house, villager.Position.Cell);
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
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, villagers.Select(villager => villager.Id).ToList());
        var ticks = 0;
        var mostAtOnce = 0;

        while (!house.IsComplete)
        {
            Assert.True(ticks < TestMatches.TickLimit);
            var before = house.BuildProgress;
            match.Tick();
            ticks++;

            // Each Villager standing beside the site adds one tick of work.
            var working = villagers.Count(villager => !villager.IsMoving && MapProbe.IsBeside(house, villager.Position.Cell));
            mostAtOnce = Math.Max(mostAtOnce, working);
            Assert.Equal(Math.Min(before + working, house.BuildTime), house.BuildProgress);
        }

        Assert.Equal(villagers.Count, mostAtOnce);
        Assert.True(ticks < house.BuildTime / 2);
        Assert.All(villagers, villager => Assert.Null(villager.ConstructionSite));
    }

    [Fact]
    public void A_Villager_sent_to_a_site_whose_side_nearest_to_it_is_walled_off_walks_round_to_another_side_and_builds_it()
    {
        var match = TestMatches.TwoPlayerMatch();
        var builder = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        Site.Stockpile(match, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        var origin = Site.OriginWalledOffOnItsNearSide(match, builder);
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, [builder.Id]));
        match.Tick();
        var site = match.State.Buildings[^1];

        Walk.UntilStopped(match, builder);
        match.Tick();

        Assert.Equal(site.Id, builder.ConstructionSite);
        Assert.True(MapProbe.IsBeside(site, builder.Position.Cell));
        Assert.True(site.BuildProgress > 0);
    }

    [Fact]
    public void A_Villager_sent_to_a_site_it_cannot_reach_stops_short_of_it_and_stands_idle()
    {
        // A map with free Cells fenced in by obstacles.
        var match = TestMatches.TwoPlayerMatch(seed: 1);
        var villager = Site.VillagersOf(match, TestMatches.FirstPlayer)[1];
        Site.Stockpile(match, TestMatches.FirstPlayer, Match.BuildingCost(BuildingKind.House).Wood);
        var reachable = MapProbe.ReachableFrom(match.State.Map, villager.Position.Cell);
        var size = Match.BuildingSize(BuildingKind.House);
        var origin = MapProbe.AllCells(match.State.Map).First(cell =>
            match.CanPlace(BuildingKind.House, cell)
            && !MapProbe.Square(new CellPosition(cell.X - 1, cell.Y - 1), size + 2).Any(reachable.Contains));
        match.Enqueue(new PlaceBuildingCommand(TestMatches.FirstPlayer, BuildingKind.House, origin, [villager.Id]));
        match.Tick();
        var house = match.State.Buildings[^1];

        Walk.UntilStopped(match, villager);

        for (var tick = 0; tick < 200; tick++)
        {
            match.Tick();
        }

        Assert.Null(villager.ConstructionSite);
        Assert.False(villager.IsMoving);
        Assert.Equal(0, house.BuildProgress);
    }

    [Fact]
    public void A_construction_site_no_Villager_builds_makes_no_progress()
    {
        var match = TestMatches.TwoPlayerMatch();
        var house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);

        for (var tick = 0; tick < 2 * house.BuildTime; tick++)
        {
            match.Tick();
        }

        Assert.Equal(0, house.BuildProgress);
        Assert.False(house.IsComplete);
    }
}
