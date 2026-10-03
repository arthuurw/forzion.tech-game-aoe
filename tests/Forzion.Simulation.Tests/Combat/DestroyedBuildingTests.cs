using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

/// <summary>What a Player loses with a building of its own destroyed in combat.</summary>
public class DestroyedBuildingTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    [Fact]
    public void Villagers_building_a_site_destroyed_in_combat_stop_building_and_stand_idle()
    {
        var match = Battle.Raiders();
        var builder = Site.VillagersOf(match, First)[1];
        var site = Site.Place(match, First, BuildingKind.House, []);
        Battle.Raid(match, site);
        Battle.TickUntil(match, () => site.HitPoints < site.MaxHitPoints);

        match.Enqueue(new BuildCommand(First, [builder.Id], site.Id));
        Battle.TickUntil(match, () => site.BuildProgress > 0);
        Battle.TickUntil(match, () => Battle.Building(match, site.Id) is null);

        Assert.False(site.IsComplete);
        Assert.Null(builder.ConstructionSite);
        Assert.False(builder.IsMoving);
        Assert.Same(builder, Battle.Unit(match, builder.Id));
    }

    [Fact]
    public void A_Villager_carrying_its_load_to_a_Storehouse_destroyed_on_the_way_carries_it_to_the_Town_Center()
    {
        var match = Battle.Raiders();
        var state = match.State;
        var townCenter = Battle.TownCenter(match, First);
        var villagers = Site.VillagersOf(match, First);
        var carrier = villagers[1];

        // A Wood source away from home, and a Storehouse halfway to it: nearer the source than the Town Center is.
        var source = Gather.NearestSource(state, TestArmies.BesideHome(match, First, 12, 8), ResourceKind.Wood);
        var home = TestArmies.BesideHome(match, First, 0, 0);
        Site.Stockpile(match, First, Match.BuildingCost(BuildingKind.Storehouse).Wood);
        var origin = Site.FreeOriginNear(
            state, new CellPosition((source.Cell.X + home.X) / 2, (source.Cell.Y + home.Y) / 2), Match.BuildingSize(BuildingKind.Storehouse));
        match.Enqueue(new PlaceBuildingCommand(First, BuildingKind.Storehouse, origin, villagers.Select(villager => villager.Id).ToList()));
        match.Tick();
        var storehouse = state.Buildings[^1];
        Battle.TickUntil(match, () => storehouse.IsComplete);

        // The carrier fills its load at the source and waits there with it.
        match.Enqueue(new GatherCommand(First, [carrier.Id], source.Id));
        Battle.TickUntil(match, () => carrier.GatherPhase == GatherPhase.ToDropOffPoint);
        Site.Halt(match, [carrier]);

        // The raiders bring the Storehouse down to its last hit.
        Battle.Raid(match, storehouse);
        var smallestHit = int.MaxValue;
        var hitPoints = storehouse.HitPoints;
        Battle.TickUntil(match, () =>
        {
            if (storehouse.HitPoints < hitPoints)
            {
                smallestHit = Math.Min(smallestHit, hitPoints - storehouse.HitPoints);
                hitPoints = storehouse.HitPoints;
            }

            return storehouse.HitPoints <= smallestHit;
        });

        match.Enqueue(new GatherCommand(First, [carrier.Id], source.Id));
        match.Tick();

        Assert.Equal(GatherPhase.ToDropOffPoint, carrier.GatherPhase);
        Assert.True(Gather.Touches(storehouse, carrier.Path[^1]));

        Battle.TickUntil(match, () => Battle.Building(match, storehouse.Id) is null);

        Assert.Equal(GatherPhase.ToDropOffPoint, carrier.GatherPhase);
        Assert.True(carrier.IsMoving);
        Assert.True(Gather.Touches(townCenter, carrier.Path[^1]));
    }

    [Fact]
    public void A_House_destroyed_in_combat_takes_back_what_it_added_to_the_population_limit()
    {
        var match = Battle.Raiders();
        var initial = match.State.PopulationLimitOf(First);
        var villagers = Site.VillagersOf(match, First).Select(villager => villager.Id).ToList();
        var house = Site.Place(match, First, BuildingKind.House, villagers);
        Battle.TickUntil(match, () => house.IsComplete);
        var withHouse = match.State.PopulationLimitOf(First);

        Battle.Raid(match, house);
        Battle.TickUntil(match, () => Battle.Building(match, house.Id) is null);

        Assert.True(withHouse > initial);
        Assert.Equal(initial, match.State.PopulationLimitOf(First));
    }
}
