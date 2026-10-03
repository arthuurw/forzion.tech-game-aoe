using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

public class AttackCommandTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void Units_and_buildings_start_with_full_hit_points()
    {
        var match = Battle.Create(first: plain =>
        [
            new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, -2, 0)),
            new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, First, 2, 0)),
        ]);

        Assert.All(match.State.Units, unit =>
        {
            Assert.True(unit.MaxHitPoints > 0);
            Assert.Equal(unit.MaxHitPoints, unit.HitPoints);
        });
        Assert.All(match.State.Buildings, building =>
        {
            Assert.True(building.MaxHitPoints > 0);
            Assert.Equal(building.MaxHitPoints, building.HitPoints);
        });
    }

    [Fact]
    public void A_ranged_soldier_hits_a_unit_within_range_from_where_it_stands_once_per_attack_interval()
    {
        // Two Cells to the side of the enemy Town Center: the middle Villager stands two Cells
        // from its centre the other way, nearly three Cells off in a straight line.
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, Second, -2, 0))]);
        var archer = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        var standing = archer.Position;
        match.Enqueue(new AttackCommand(First, [archer.Id], villager.Id));

        // Hit points after each tick, starting with those before the first.
        var hitPoints = new List<int> { villager.HitPoints };

        while (Hits().Count < 3 && hitPoints.Count < Battle.TickLimit)
        {
            match.Tick();
            hitPoints.Add(villager.HitPoints);
        }

        var hits = Hits();
        var interval = hits[0];

        Assert.Equal(villager.MaxHitPoints, hitPoints[0]);
        Assert.Equal(3, hits.Count);
        Assert.True(interval > 1, "The first hit lands at the end of an attack interval, not at once.");
        Assert.Equal([interval, 2 * interval, 3 * interval], hits);
        Assert.Single(hits.Select(tick => hitPoints[tick - 1] - hitPoints[tick]).Distinct());
        Assert.Equal(standing, archer.Position);
        Assert.Equal(villager.Id, archer.Target);

        // The ticks at which a hit landed, counted from the one that applied the command.
        List<int> Hits() =>
            Enumerable.Range(1, hitPoints.Count - 1).Where(tick => hitPoints[tick] < hitPoints[tick - 1]).ToList();
    }

    [Fact]
    public void An_attack_on_an_entity_that_is_neither_a_unit_nor_a_building_is_rejected()
    {
        var match = MatchWithSoldier();
        var soldier = Battle.Last(match);

        AssertRejected(match, new AttackCommand(First, [soldier.Id], match.State.ResourceSources[0].Id), RejectionReason.UnknownTarget);
        AssertRejected(match, new AttackCommand(First, [soldier.Id], new EntityId(100_000)), RejectionReason.UnknownTarget);
    }

    [Fact]
    public void An_attack_on_the_Players_own_unit_or_building_is_rejected()
    {
        var match = MatchWithSoldier();
        var soldier = Battle.Last(match);

        AssertRejected(match, new AttackCommand(First, [soldier.Id], Battle.MiddleVillager(match, First).Id), RejectionReason.OwnTarget);
        AssertRejected(match, new AttackCommand(First, [soldier.Id], Battle.TownCenter(match, First).Id), RejectionReason.OwnTarget);
    }

    [Fact]
    public void An_attack_naming_a_unit_that_does_not_exist_still_sends_the_others()
    {
        var match = MatchWithSoldier();
        var soldier = Battle.Last(match);
        var target = Battle.TownCenter(match, Second).Id;
        match.Enqueue(new AttackCommand(First, [new EntityId(100_000), soldier.Id], target));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(target, soldier.Target);
    }

    [Fact]
    public void An_attack_naming_no_unit_that_exists_is_rejected()
    {
        var match = MatchWithSoldier();
        var target = Battle.TownCenter(match, Second).Id;

        AssertRejected(match, new AttackCommand(First, [new EntityId(100_000)], target), RejectionReason.UnknownUnit);
    }

    [Fact]
    public void A_unit_that_died_after_the_attack_was_ordered_is_skipped_and_the_others_attack()
    {
        // The second Player's two soldiers stand beside the first Player's melee soldier and
        // strike it down together; its ranged soldier stands a Cell further off.
        var match = Battle.Create(
            first: plain =>
            [
                new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 0)),
                new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, Second, -2, 1)),
            ],
            second: plain =>
            [
                new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, -1)),
                new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, -2)),
            ]);
        var units = match.State.UnitsOf(First).Where(unit => unit.Kind != UnitKind.Villager).ToList();
        var melee = units[0];
        var archer = units[1];
        var townCenter = Battle.TownCenter(match, Second).Id;

        // The order is given while the melee soldier still stands, and reaches the match after it fell.
        var command = new AttackCommand(First, [melee.Id, archer.Id], townCenter);
        Battle.TickUntil(match, () => melee.HitPoints <= 0 || archer.HitPoints <= 0);
        Assert.Null(Battle.Unit(match, melee.Id));
        match.Enqueue(command);
        match.Tick();

        Assert.DoesNotContain(match.Events, matchEvent => matchEvent is CommandRejected);
        Assert.Equal(townCenter, archer.Target);
    }

    [Fact]
    public void An_attack_by_another_Players_unit_is_rejected_and_sends_none_of_its_units()
    {
        var match = MatchWithSoldier();
        var soldier = Battle.Last(match);
        var foreign = Battle.MiddleVillager(match, Second);
        var target = Battle.TownCenter(match, Second).Id;

        AssertRejected(match, new AttackCommand(First, [soldier.Id, foreign.Id], target), RejectionReason.UnitOfAnotherPlayer);
        Assert.Null(soldier.Target);
    }

    [Fact]
    public void An_attack_by_soldiers_and_Villagers_sends_the_soldiers_and_leaves_the_Villagers_to_what_they_were_doing()
    {
        var match = MatchWithSoldier();
        var soldier = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, First);
        var source = Gather.NearestSource(match.State, villager.Position.Cell, ResourceKind.Wood);
        var target = Battle.TownCenter(match, Second).Id;
        match.Enqueue(new GatherCommand(First, [villager.Id], source.Id));
        match.Tick();

        match.Enqueue(new AttackCommand(First, [villager.Id, soldier.Id], target));
        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(target, soldier.Target);
        Assert.Null(villager.Target);
        Assert.Equal(source.Id, villager.GatherSource);
        Assert.NotEqual(GatherPhase.None, villager.GatherPhase);
    }

    [Fact]
    public void An_attack_by_Villagers_alone_is_rejected()
    {
        var match = MatchWithSoldier();
        var villager = Battle.MiddleVillager(match, First);
        var target = Battle.TownCenter(match, Second).Id;

        AssertRejected(match, new AttackCommand(First, [villager.Id], target), RejectionReason.UnitCannotAttack);
        Assert.Null(villager.Target);
    }

    [Fact]
    public void A_rejected_attack_leaves_the_state_as_if_it_had_not_been_sent()
    {
        var withRejection = MatchWithSoldier();
        var without = MatchWithSoldier();
        withRejection.Enqueue(new AttackCommand(
            First, [Battle.Last(withRejection).Id], Battle.TownCenter(withRejection, First).Id));

        withRejection.Tick();
        without.Tick();

        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void An_attack_makes_the_hash_diverge_from_a_match_without_it()
    {
        var withAttack = MatchWithSoldier();
        var without = MatchWithSoldier();
        withAttack.Enqueue(new AttackCommand(
            First, [Battle.Last(withAttack).Id], Battle.TownCenter(withAttack, Second).Id));

        withAttack.Tick();
        without.Tick();

        Assert.NotEqual(without.StateHash, withAttack.StateHash);
    }

    /// <summary>A match in which the first Player has a melee soldier beside its own Town Center.</summary>
    private static Match MatchWithSoldier() =>
        Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, -2, 0))]);

    private static void AssertRejected(Match match, AttackCommand command, RejectionReason reason)
    {
        match.Enqueue(command);
        match.Tick();

        Assert.Equal([new CommandRejected(command, reason)], match.Events);
    }
}
