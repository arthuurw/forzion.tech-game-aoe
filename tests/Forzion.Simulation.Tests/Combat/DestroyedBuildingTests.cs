using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

/// <summary>What a Player loses with a building of its own destroyed in combat.</summary>
public class DestroyedBuildingTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void Villagers_building_a_site_destroyed_in_combat_stop_building_and_stand_idle()
    {
        var match = Raiders();
        var builder = Site.VillagersOf(match, First)[1];
        var site = Site.Place(match, First, BuildingKind.House, []);
        Raid(match, site);
        Battle.TickUntil(match, () => site.HitPoints < site.MaxHitPoints);

        match.Enqueue(new BuildCommand(First, [builder.Id], site.Id));
        Battle.TickUntil(match, () => site.BuildProgress > 0);
        Battle.TickUntil(match, () => Battle.Building(match, site.Id) is null);

        Assert.False(site.IsComplete);
        Assert.Null(builder.ConstructionSite);
        Assert.False(builder.IsMoving);
        Assert.Same(builder, Battle.Unit(match, builder.Id));
    }

    /// <summary>
    /// The default match, with the second Player starting with melee soldiers beside its own
    /// Town Center, far from the first Player's.
    /// </summary>
    private static Match Raiders() => Battle.Create(
        second: plain =>
        [
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, 2, -1)),
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, 2, 0)),
            new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, 2, 1)),
        ]);

    /// <summary>Orders the second Player's soldiers to attack the building.</summary>
    private static void Raid(Match match, BuildingState building) =>
        match.Enqueue(new AttackCommand(
            Second,
            match.State.Units.Where(unit => unit.Owner == Second && unit.Kind == UnitKind.MeleeSoldier).Select(unit => unit.Id).ToList(),
            building.Id));
}
