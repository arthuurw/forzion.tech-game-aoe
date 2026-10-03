using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

public class ConstructionHashTests
{
    [Fact]
    public void A_Villager_walking_to_build_and_one_walking_the_same_way_unordered_have_different_hashes()
    {
        var building = WithHouse(out var house);
        var walking = WithHouse(out _);
        var villager = Site.VillagersOf(building, TestMatches.FirstPlayer)[1];
        building.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [villager.Id], house.Id));
        building.Tick();

        // The other match sends the same Villager walking to where the build order took it.
        walking.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], villager.Path[^1]));
        walking.Tick();
        var walker = Site.VillagersOf(walking, TestMatches.FirstPlayer)[1];

        Assert.True(villager.IsMoving);
        Assert.Equal(walker.Path, villager.Path);
        Assert.Equal(walker.Position, villager.Position);
        Assert.Equal(house.Id, villager.ConstructionSite);
        Assert.Null(walker.ConstructionSite);
        Assert.NotEqual(walking.StateHash, building.StateHash);
    }

    [Fact]
    public void Sites_alike_but_for_how_much_work_they_received_have_different_hashes()
    {
        var earlier = BesideHouse(out var house);
        var later = BesideHouse(out _);
        var villager = Walk.MiddleVillager(earlier);
        earlier.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [villager.Id], house.Id));
        earlier.Tick();
        later.Tick();
        later.Enqueue(new BuildCommand(TestMatches.FirstPlayer, [villager.Id], house.Id));
        later.Tick();
        earlier.Tick();

        var other = Walk.MiddleVillager(later);
        Assert.Equal(other.Position, villager.Position);
        Assert.Equal(other.ConstructionSite, villager.ConstructionSite);
        Assert.NotEqual(later.State.Buildings[^1].BuildProgress, house.BuildProgress);
        Assert.NotEqual(later.StateHash, earlier.StateHash);
    }

    [Fact]
    public void Matches_given_the_same_construction_orders_have_the_same_hash_at_every_tick()
    {
        var first = WithHouse(out var house);
        var second = WithHouse(out _);
        var villagers = Site.VillagersOf(first, TestMatches.FirstPlayer).Select(villager => villager.Id).ToList();
        first.Enqueue(new BuildCommand(TestMatches.FirstPlayer, villagers, house.Id));
        second.Enqueue(new BuildCommand(TestMatches.FirstPlayer, villagers, house.Id));

        for (var tick = 0; !house.IsComplete; tick++)
        {
            Assert.True(tick < Gather.TickLimit);
            first.Tick();
            second.Tick();

            Assert.Equal(first.StateHash, second.StateHash);
        }
    }

    /// <summary>A match in which the first Player has placed a House near its Town Center, with no builder.</summary>
    private static Match WithHouse(out BuildingState house)
    {
        var match = TestMatches.TwoPlayerMatch();
        house = Site.Place(match, TestMatches.FirstPlayer, BuildingKind.House, []);

        return match;
    }

    /// <summary>A match with a House site of the first Player and its middle Villager standing still beside it.</summary>
    private static Match BesideHouse(out BuildingState house)
    {
        var match = WithHouse(out house);
        var villager = Walk.MiddleVillager(match);
        var beside = new CellPosition(house.Origin.X - 1, house.Origin.Y);
        match.Enqueue(new MoveCommand(TestMatches.FirstPlayer, [villager.Id], beside));
        Walk.UntilStopped(match, villager);

        return match;
    }
}
