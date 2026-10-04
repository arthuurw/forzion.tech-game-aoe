using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

public class AutomaticAttackTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void An_idle_soldier_attacks_an_enemy_unit_that_comes_near()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        var startingDistance = Battle.Distance(soldier.Position, villager.Position);
        match.Enqueue(new MoveCommand(Second, [villager.Id], TestArmies.BesideHome(match, First, 2, 1)));

        Battle.TickUntil(match, () => soldier.Target is not null);
        var noticedAt = Battle.Distance(soldier.Position, villager.Position);
        Battle.TickUntil(match, () => villager.HitPoints < villager.MaxHitPoints);

        Assert.True(startingDistance > 20);
        Assert.Equal(villager.Id, soldier.Target);
        Assert.True(noticedAt > 1, "The soldier noticed the Villager before it walked into reach.");
    }

    [Fact]
    public void An_idle_soldier_ignores_enemies_far_away()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);

        Battle.Run(match, 100);

        Assert.Null(soldier.Target);
        Assert.All(match.State.Units, unit => Assert.Equal(unit.MaxHitPoints, unit.HitPoints));
    }

    [Fact]
    public void A_walking_soldier_does_not_stop_for_enemies_and_attacks_once_it_stands_still()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);
        var destination = TestArmies.BesideHome(match, Second, -2, 0);
        match.Enqueue(new MoveCommand(First, [soldier.Id], destination));

        Battle.TickUntil(match, () =>
        {
            Assert.Null(soldier.Target);

            return !soldier.IsMoving;
        });
        var stoppedAt = soldier.Position;
        match.Tick();

        Assert.Equal(MapPosition.CentreOf(destination), stoppedAt);
        Assert.NotNull(soldier.Target);
        Assert.Equal(Second, Battle.Unit(match, soldier.Target.Value)!.Owner);
    }

    [Fact]
    public void An_idle_soldier_attacks_the_nearest_enemy_unit()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 1))]);
        var soldier = Battle.Last(match);

        // Nearest first; between enemies equally near, the one with the lowest ID.
        var nearest = match.State.Units
            .Where(unit => unit.Owner == Second)
            .OrderBy(unit => Battle.Distance(soldier.Position, unit.Position))
            .ThenBy(unit => unit.Id.Value)
            .First();

        match.Tick();

        Assert.Equal(nearest.Id, soldier.Target);
    }

    [Fact]
    public void Villagers_do_not_attack_on_their_own()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 1))]);

        Battle.Run(match, 100);

        Assert.All(match.State.Units.Where(unit => unit.Kind == UnitKind.Villager), villager => Assert.Null(villager.Target));
        Assert.Equal(Battle.Last(match).MaxHitPoints, Battle.Last(match).HitPoints);
    }

    [Fact]
    public void An_idle_soldier_attacks_an_enemy_building_when_no_enemy_unit_is_near()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);
        var townCenter = Battle.TownCenter(match, Second);
        var enemyVillagers = match.State.Units.Where(unit => unit.Owner == Second).Select(unit => unit.Id).ToList();

        // The enemy Villagers leave for a corner far from both homes, then the soldier walks up to their Town Center.
        match.Enqueue(new MoveCommand(Second, enemyVillagers, new CellPosition(0, match.State.Map.Height - 1)));
        Battle.TickUntil(match, () => match.State.Units.All(unit => !unit.IsMoving));
        match.Enqueue(new MoveCommand(First, [soldier.Id], TestArmies.BesideHome(match, Second, -2, 0)));
        Battle.TickUntil(match, () => !soldier.IsMoving);
        Battle.TickUntil(match, () => townCenter.HitPoints < townCenter.MaxHitPoints);

        Assert.All(
            enemyVillagers,
            id => Assert.True(Battle.Distance(soldier.Position, Battle.Unit(match, id)!.Position) > 10));
        Assert.Equal(townCenter.Id, soldier.Target);
    }

    [Fact]
    public void An_idle_soldier_attacks_an_enemy_unit_before_a_nearer_enemy_building()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 1))]);
        var soldier = Battle.Last(match);
        var villagerDistance = match.State.Units
            .Where(unit => unit.Owner == Second)
            .Min(unit => Battle.Distance(soldier.Position, unit.Position));

        match.Tick();

        // The soldier stands beside the Town Center, half a Cell from its footprint.
        Assert.True(villagerDistance > 1);
        Assert.Equal(Second, Battle.Unit(match, soldier.Target!.Value)!.Owner);
    }

    [Fact]
    public void An_idle_soldier_ignores_its_own_Players_buildings()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);

        Battle.Run(match, 100);

        Assert.Null(soldier.Target);
        Assert.All(match.State.Buildings, building => Assert.Equal(building.MaxHitPoints, building.HitPoints));
    }
}
