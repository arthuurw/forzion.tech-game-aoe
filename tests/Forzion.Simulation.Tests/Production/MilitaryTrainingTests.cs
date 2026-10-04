using Forzion.Simulation.Tests.Combat;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class MilitaryTrainingTests
{
    [Fact]
    public void A_Barracks_trains_the_soldiers_of_its_queue_in_the_order_they_were_queued()
    {
        var match = TestMatches.TwoPlayerMatch();
        var barracks = Train.Complete(match, TestMatches.FirstPlayer, BuildingKind.Barracks);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.RangedSoldier));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.MeleeSoldier));
        var start = match.State.Tick;

        var trained = Train.Run(match, Match.TrainTime(UnitKind.RangedSoldier) + Match.TrainTime(UnitKind.MeleeSoldier))
            .Where(happened => happened.Event is UnitTrained)
            .ToList();

        Assert.Equal(
            [start + Match.TrainTime(UnitKind.RangedSoldier), start + Match.TrainTime(UnitKind.RangedSoldier) + Match.TrainTime(UnitKind.MeleeSoldier)],
            trained.Select(happened => happened.Tick));
        Assert.Equal(
            [UnitKind.RangedSoldier, UnitKind.MeleeSoldier],
            trained.Select(happened => Battle.Unit(match, ((UnitTrained)happened.Event).Unit)!.Kind));
        Assert.All(trained, happened => Assert.Equal(barracks.Id, ((UnitTrained)happened.Event).Building));
    }

    [Fact]
    public void Queues_alike_but_for_the_order_of_their_units_have_different_hashes()
    {
        var first = WithBarracks(out var barracks);
        var second = WithBarracks(out _);
        first.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.MeleeSoldier));
        first.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.RangedSoldier));
        second.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.RangedSoldier));
        second.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.MeleeSoldier));

        first.Tick();
        second.Tick();

        Assert.Equal(Train.Stock(first.State.Players[0]), Train.Stock(second.State.Players[0]));
        Assert.NotEqual(first.StateHash, second.StateHash);
    }

    [Fact]
    public void A_Barracks_trains_a_heavy_soldier()
    {
        var match = TestMatches.TwoPlayerMatch();
        var barracks = Train.Complete(match, TestMatches.FirstPlayer, BuildingKind.Barracks);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, barracks.Id, UnitKind.HeavySoldier));

        var trained = Train.Run(match, Match.TrainTime(UnitKind.HeavySoldier)).Select(happened => happened.Event).OfType<UnitTrained>().Single();

        var soldier = Battle.Unit(match, trained.Unit)!;
        Assert.Equal(UnitKind.HeavySoldier, soldier.Kind);
        Assert.Equal(TestMatches.FirstPlayer, soldier.Owner);
    }

    [Fact]
    public void A_heavy_soldier_beats_a_melee_soldier_in_single_combat()
    {
        var match = Battle.Create(
            first: plain => [new(UnitKind.HeavySoldier, TestArmies.BesideHome(plain, TestMatches.FirstPlayer, -2, 0))],
            second: plain => [new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, TestMatches.FirstPlayer, -2, 1))]);
        var heavy = match.State.Units.Single(unit => unit.Kind == UnitKind.HeavySoldier);
        var melee = match.State.Units.Single(unit => unit.Kind == UnitKind.MeleeSoldier);

        Assert.True(heavy.MaxHitPoints > melee.MaxHitPoints);

        TestMatches.TickUntil(match, () => Battle.Unit(match, melee.Id) is null || Battle.Unit(match, heavy.Id) is null);

        Assert.Same(heavy, Battle.Unit(match, heavy.Id));
        Assert.Null(Battle.Unit(match, melee.Id));
    }

    /// <summary>The default match, in which the first Player has a complete Barracks.</summary>
    private static Match WithBarracks(out BuildingState barracks)
    {
        var match = TestMatches.TwoPlayerMatch();
        barracks = Train.Complete(match, TestMatches.FirstPlayer, BuildingKind.Barracks);

        return match;
    }
}
