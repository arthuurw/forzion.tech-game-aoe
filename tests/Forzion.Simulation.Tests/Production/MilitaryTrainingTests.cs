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

    /// <summary>The default match, in which the first Player has a complete Barracks.</summary>
    private static Match WithBarracks(out BuildingState barracks)
    {
        var match = TestMatches.TwoPlayerMatch();
        barracks = Train.Complete(match, TestMatches.FirstPlayer, BuildingKind.Barracks);

        return match;
    }
}
