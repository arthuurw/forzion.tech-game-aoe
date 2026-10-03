using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

public class DeathTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void A_unit_left_without_hit_points_is_removed_in_the_same_tick_and_reported_destroyed()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, Second, -2, 0))]);
        var archer = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        match.Enqueue(new AttackCommand(First, [archer.Id], villager.Id));

        Battle.TickUntil(match, () => villager.HitPoints <= 0);

        Assert.Null(Battle.Unit(match, villager.Id));
        Assert.Contains(new EntityDestroyed(villager.Id), match.Events);
        Assert.Null(archer.Target);
        Assert.False(archer.IsMoving);
    }

    [Fact]
    public void A_unit_still_standing_is_not_reported_destroyed()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, Second, -2, 0))]);
        var archer = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        match.Enqueue(new AttackCommand(First, [archer.Id], villager.Id));

        var events = new List<MatchEvent>();
        Battle.TickUntil(match, () =>
        {
            events.AddRange(match.Events);

            return villager.HitPoints < villager.MaxHitPoints;
        });

        Assert.DoesNotContain(events, matchEvent => matchEvent is EntityDestroyed);
        Assert.Same(villager, Battle.Unit(match, villager.Id));
    }

    [Fact]
    public void Two_soldiers_that_strike_each_other_down_in_the_same_tick_both_die()
    {
        var match = Battle.Create(
            first: plain => [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 0))],
            second: plain => [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 1))]);
        var attacker = match.State.Units[^2];
        var defender = match.State.Units[^1];
        match.Enqueue(new AttackCommand(First, [attacker.Id], defender.Id));
        match.Enqueue(new AttackCommand(Second, [defender.Id], attacker.Id));

        Battle.TickUntil(match, () => attacker.HitPoints <= 0 || defender.HitPoints <= 0);

        // Units act in ID order within a tick, yet acting first is no advantage: both hits land.
        Assert.Equal(attacker.HitPoints, defender.HitPoints);
        Assert.Null(Battle.Unit(match, attacker.Id));
        Assert.Null(Battle.Unit(match, defender.Id));
        Assert.Equal([new EntityDestroyed(attacker.Id), new EntityDestroyed(defender.Id)], match.Events);
    }
}
