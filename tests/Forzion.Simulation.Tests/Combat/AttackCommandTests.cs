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
}
