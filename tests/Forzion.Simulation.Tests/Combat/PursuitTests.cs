using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Combat;

public class PursuitTests
{
    private static readonly PlayerId First = TestMatches.FirstPlayer;
    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    [Fact]
    public void A_melee_soldier_walks_up_to_a_target_out_of_its_reach_before_hitting_it()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 0))]);
        var soldier = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        var start = soldier.Position;
        match.Enqueue(new AttackCommand(First, [soldier.Id], villager.Id));

        Battle.TickUntil(match, () => villager.HitPoints < villager.MaxHitPoints);

        // The two started nearly three Cells apart; a melee soldier strikes from beside its target.
        Assert.True(Battle.Distance(start, villager.Position) > 2);
        Assert.True(Battle.Distance(soldier.Position, villager.Position) <= 1);
        Assert.False(soldier.IsMoving);
    }

    [Fact]
    public void A_soldier_chases_a_target_that_walks_away()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, Second, -2, 0))]);
        var soldier = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        var start = villager.Position;
        var middleOfMap = new CellPosition(match.State.Map.Width / 2, match.State.Map.Height / 2);
        match.Enqueue(new MoveCommand(Second, [villager.Id], middleOfMap));
        match.Enqueue(new AttackCommand(First, [soldier.Id], villager.Id));

        Battle.TickUntil(match, () => villager.HitPoints < villager.MaxHitPoints);

        // The villager walked off towards the middle of the map, away from the soldier.
        Assert.True(Battle.Distance(start, villager.Position) > 10);
        Assert.True(Battle.Distance(soldier.Position, villager.Position) <= 1);
    }

    [Fact]
    public void A_ranged_soldier_closes_in_only_until_its_target_is_within_range()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var archer = Battle.Last(match);
        var villager = Battle.MiddleVillager(match, Second);
        var start = archer.Position;
        match.Enqueue(new AttackCommand(First, [archer.Id], villager.Id));

        Battle.TickUntil(match, () => villager.HitPoints < villager.MaxHitPoints);

        // It crossed the map, yet hits from well beyond a melee soldier's reach.
        Assert.True(Battle.Distance(start, villager.Position) > 20);
        Assert.True(Battle.Distance(archer.Position, villager.Position) > 2);
        Assert.False(archer.IsMoving);
    }

    [Fact]
    public void A_move_order_calls_off_an_attack()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);
        var townCenter = Battle.TownCenter(match, Second);
        var home = soldier.Position.Cell;
        match.Enqueue(new AttackCommand(First, [soldier.Id], townCenter.Id));

        for (var tick = 0; tick < 40; tick++)
        {
            match.Tick();
        }

        match.Enqueue(new MoveCommand(First, [soldier.Id], home));
        Battle.TickUntil(match, () => !soldier.IsMoving);
        Battle.Run(match, 100);

        Assert.Null(soldier.Target);
        Assert.Equal(MapPosition.CentreOf(home), soldier.Position);
        Assert.Equal(townCenter.MaxHitPoints, townCenter.HitPoints);
    }

    [Fact]
    public void A_melee_soldier_hits_a_building_from_beside_its_footprint()
    {
        var match = Battle.Create(first: plain =>
            [new StartingUnit(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, First, 2, 0))]);
        var soldier = Battle.Last(match);
        var townCenter = Battle.TownCenter(match, Second);
        match.Enqueue(new AttackCommand(First, [soldier.Id], townCenter.Id));

        Battle.TickUntil(match, () => townCenter.HitPoints < townCenter.MaxHitPoints);

        var cell = soldier.Position.Cell;
        Assert.Equal(CellKind.Free, match.State.Map[cell]);
        Assert.True(cell.X >= townCenter.Origin.X - 1 && cell.X <= townCenter.Origin.X + townCenter.Width);
        Assert.True(cell.Y >= townCenter.Origin.Y - 1 && cell.Y <= townCenter.Origin.Y + townCenter.Height);
        Assert.False(soldier.IsMoving);
    }
}
