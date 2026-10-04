using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Combat;

/// <summary>What a Player loses with a building of its own destroyed in combat.</summary>
public class DestroyedBuildingTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;

    [Fact]
    public void Villagers_building_a_site_destroyed_in_combat_stop_building_and_stand_idle()
    {
        var (match, site, builder) = BuilderAtARaidedSite();

        TestMatches.TickUntil(match, () => Battle.Building(match, site.Id) is null);

        Assert.False(site.IsComplete);
        Assert.Null(builder.ConstructionSite);
        Assert.False(builder.IsMoving);
        Assert.Same(builder, Battle.Unit(match, builder.Id));
    }

    // Combat runs before construction: a site is gone before its builders would work on it.
    [Fact]
    public void A_site_destroyed_in_a_tick_gets_no_work_from_its_builders_in_that_tick()
    {
        var (match, site, _) = BuilderAtARaidedSite();
        var progress = site.BuildProgress;

        TestMatches.TickUntil(match, () =>
        {
            if (Battle.Building(match, site.Id) is null)
            {
                return true;
            }

            progress = site.BuildProgress;

            return false;
        });

        Assert.Equal(progress, site.BuildProgress);
    }

    [Fact]
    public void A_Villager_carrying_its_load_to_a_Storehouse_destroyed_on_the_way_carries_it_to_the_Town_Center()
    {
        var match = Battle.Raiders();
        var townCenter = Battle.TownCenter(match, First);
        var (storehouse, carrier, source) = StorehouseAwayFromHome(match);

        // The raiders bring the Storehouse down to its last hit.
        Battle.Raid(match, storehouse);
        var smallestHit = int.MaxValue;
        var hitPoints = storehouse.HitPoints;
        TestMatches.TickUntil(match, () =>
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
        Assert.True(MapProbe.IsBeside(storehouse, carrier.Path[^1]));

        TestMatches.TickUntil(match, () => Battle.Building(match, storehouse.Id) is null);

        Assert.Equal(GatherPhase.ToDropOffPoint, carrier.GatherPhase);
        Assert.True(carrier.IsMoving);
        Assert.True(MapProbe.IsBeside(townCenter, carrier.Path[^1]));
    }

    // A carrier already beside its drop-off point stands still until the next tick hands the load over.
    [Fact]
    public void A_Villager_waiting_beside_a_Storehouse_destroyed_in_that_tick_carries_its_load_to_the_Town_Center_and_keeps_its_source()
    {
        // The match is deterministic: a first run tells how many ticks the raid takes to bring the Storehouse down.
        var probe = StorehouseUnderRaid();
        var ticksToDestroy = TestMatches.TickUntil(probe.Match, () => Battle.Building(probe.Match, probe.Storehouse.Id) is null);

        var (match, storehouse, carrier, source) = StorehouseUnderRaid();
        var townCenter = Battle.TownCenter(match, First);
        var load = carrier.Load;
        Assert.True(MapProbe.IsBeside(storehouse, carrier.Position.Cell));
        Assert.False(MapProbe.IsBeside(townCenter, carrier.Position.Cell));

        for (var tick = 1; tick < ticksToDestroy; tick++)
        {
            match.Tick();
        }

        // The order makes the carrier wait beside the Storehouse with an empty path, as a load filling there does.
        match.Enqueue(new GatherCommand(First, [carrier.Id], source.Id));
        match.Tick();

        Assert.Null(Battle.Building(match, storehouse.Id));
        Assert.Equal(GatherPhase.ToDropOffPoint, carrier.GatherPhase);
        Assert.True(carrier.IsMoving);
        Assert.True(MapProbe.IsBeside(townCenter, carrier.Path[^1]));
        Assert.Equal(source.Id, carrier.GatherSource);
        Assert.Equal(load, carrier.Load);
    }

    [Fact]
    public void A_House_destroyed_in_combat_takes_back_what_it_added_to_the_population_limit()
    {
        var match = Battle.Raiders();
        var initial = match.State.PopulationLimitOf(First);
        var villagers = Site.VillagersOf(match, First).Select(villager => villager.Id).ToList();
        var house = Site.Place(match, First, BuildingKind.House, villagers);
        TestMatches.TickUntil(match, () => house.IsComplete);
        var withHouse = match.State.PopulationLimitOf(First);

        Battle.Raid(match, house);
        TestMatches.TickUntil(match, () => Battle.Building(match, house.Id) is null);

        Assert.True(withHouse > initial);
        Assert.Equal(initial, match.State.PopulationLimitOf(First));
    }

    /// <summary>
    /// The raiders' match, in which the first Player's Storehouse stands halfway to a Wood source
    /// away from home and the raiders have just been ordered to attack it, while a Villager with
    /// a full load of Wood stands idle beside it.
    /// </summary>
    private static (Match Match, BuildingState Storehouse, UnitState Carrier, ResourceSourceState Source) StorehouseUnderRaid()
    {
        var match = Battle.Raiders();
        var (storehouse, carrier, source) = StorehouseAwayFromHome(match);

        // The ring of Cells around a site placed by FreeOriginNear is free; on its side away from home it is not beside the Town Center.
        var beside = new CellPosition(storehouse.Origin.X + storehouse.Width, storehouse.Origin.Y);
        match.Enqueue(new MoveCommand(First, [carrier.Id], beside));
        Walk.UntilStopped(match, carrier);
        Battle.Raid(match, storehouse);

        return (match, storehouse, carrier, source);
    }

    /// <summary>
    /// The raiders' match, in which the first Player's middle Villager is building an
    /// unfinished House that the raiders have started to attack.
    /// </summary>
    private static (Match Match, BuildingState Site, UnitState Builder) BuilderAtARaidedSite()
    {
        var match = Battle.Raiders();
        var builder = Site.VillagersOf(match, First)[1];
        var site = Site.Place(match, First, BuildingKind.House, []);
        Battle.Raid(match, site);
        TestMatches.TickUntil(match, () => site.HitPoints < site.MaxHitPoints);
        match.Enqueue(new BuildCommand(First, [builder.Id], site.Id));
        TestMatches.TickUntil(match, () => site.BuildProgress > 0);

        return (match, site, builder);
    }

    /// <summary>
    /// Has the first Player's Villagers build a Storehouse halfway to a Wood source away from
    /// home, nearer the source than the Town Center is, then has the middle Villager fill its
    /// load at that source and stand there with it.
    /// </summary>
    private static (BuildingState Storehouse, UnitState Carrier, ResourceSourceState Source) StorehouseAwayFromHome(Match match)
    {
        var state = match.State;
        var villagers = Site.VillagersOf(match, First);
        var carrier = villagers[1];
        var source = Gather.NearestSource(state, TestArmies.BesideHome(match, First, 12, 8), ResourceKind.Wood);
        var home = TestArmies.BesideHome(match, First, 0, 0);
        Site.Stockpile(match, First, Match.BuildingCost(BuildingKind.Storehouse).Wood);
        var origin = Site.FreeOriginNear(
            state, new CellPosition((source.Cell.X + home.X) / 2, (source.Cell.Y + home.Y) / 2), Match.BuildingSize(BuildingKind.Storehouse));
        match.Enqueue(new PlaceBuildingCommand(First, BuildingKind.Storehouse, origin, villagers.Select(villager => villager.Id).ToList()));
        match.Tick();
        var storehouse = state.Buildings[^1];
        TestMatches.TickUntil(match, () => storehouse.IsComplete);

        match.Enqueue(new GatherCommand(First, [carrier.Id], source.Id));
        TestMatches.TickUntil(match, () => carrier.GatherPhase == GatherPhase.ToDropOffPoint);
        Site.Halt(match, [carrier]);

        return (storehouse, carrier, source);
    }
}
