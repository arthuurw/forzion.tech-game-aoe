using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

public class BuildingDestructionTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void A_building_left_without_hit_points_is_removed_in_the_same_tick_and_reported_destroyed()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);

        Battle.TickUntil(match, () => townCenter.HitPoints <= 0);

        Assert.Null(Battle.Building(match, townCenter.Id));
        Assert.Contains(new EntityDestroyed(townCenter.Id), match.Events);
        Assert.All(Battle.Besiegers(match), soldier => Assert.Null(soldier.Target));
    }

    [Fact]
    public void A_destroyed_building_frees_its_Cells_for_units_to_walk_on()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);
        var footprint = MapProbe.Footprint(townCenter).ToList();
        var middle = footprint[footprint.Count / 2];
        Battle.TickUntil(match, () => townCenter.HitPoints <= 0);

        var soldier = Battle.Besiegers(match)[0];
        match.Enqueue(new MoveCommand(First, [soldier.Id], middle));
        Battle.TickUntil(match, () => !soldier.IsMoving);

        Assert.All(footprint, cell => Assert.Equal(CellKind.Free, match.State.Map[cell]));
        Assert.Equal(MapPosition.CentreOf(middle), soldier.Position);
    }

    [Fact]
    public void A_gathering_Villager_left_without_a_drop_off_point_stands_idle_keeping_its_load()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);
        var villager = Battle.MiddleVillager(match, Second);
        var player = match.State.Players[1];
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Food);
        match.Enqueue(new GatherCommand(Second, [villager.Id], source.Id));
        Battle.TickUntil(match, () => townCenter.HitPoints <= 0);

        // The besiegers leave for home at once, before they turn on the Villagers.
        match.Enqueue(new MoveCommand(
            First, Battle.Besiegers(match).Select(soldier => soldier.Id).ToList(), TestArmies.BesideHome(match, First, 2, 0)));
        var food = player.AmountOf(ResourceKind.Food);
        Battle.TickUntil(match, () => villager.GatherPhase == GatherPhase.None);
        var load = villager.Load;
        Battle.Run(match, 200);

        Assert.Equal(ResourceKind.Food, load.Resource);
        Assert.True(load.Amount > 0);
        Assert.Equal(load, villager.Load);
        Assert.Null(villager.GatherSource);
        Assert.False(villager.IsMoving);
        Assert.Equal(food, player.AmountOf(ResourceKind.Food));
        Assert.Same(villager, Battle.Unit(match, villager.Id));
    }

    [Fact]
    public void A_building_still_standing_keeps_its_Cells()
    {
        var match = Battle.Siege();
        var townCenter = Battle.TownCenter(match, Second);

        Battle.TickUntil(match, () => townCenter.HitPoints < townCenter.MaxHitPoints);

        Assert.Same(townCenter, Battle.Building(match, townCenter.Id));
        Assert.All(MapProbe.Footprint(townCenter), cell => Assert.Equal(CellKind.Building, match.State.Map[cell]));
    }
}
