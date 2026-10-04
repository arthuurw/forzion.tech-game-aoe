using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class CancelTrainingCommandTests
{
    [Fact]
    public void Cancelling_a_queued_unit_takes_it_off_the_queue_and_gives_back_its_whole_cost()
    {
        var match = TestMatches.TwoPlayerMatch();
        var player = match.State.Players[0];
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        var before = Train.Stock(player);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        Train.Run(match, Match.TrainTime(UnitKind.Villager) / 2);

        Assert.NotEqual(before, Train.Stock(player));

        match.Enqueue(new CancelTrainingCommand(TestMatches.FirstPlayer, townCenter.Id, 0));
        match.Tick();

        Assert.Empty(match.Events);
        Assert.Empty(townCenter.TrainingQueue);
        Assert.Equal(0, townCenter.TrainingProgress);
        Assert.Equal(before, Train.Stock(player));

        var units = match.State.Units.Count;
        Train.Run(match, Match.TrainTime(UnitKind.Villager));

        Assert.Equal(units, match.State.Units.Count);
    }

    [Fact]
    public void Cancelling_the_unit_in_training_starts_the_next_one_from_the_beginning()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        Train.Run(match, Match.TrainTime(UnitKind.Villager) / 2);
        match.Enqueue(new CancelTrainingCommand(TestMatches.FirstPlayer, townCenter.Id, 0));

        match.Tick();

        Assert.Equal([UnitKind.Villager], townCenter.TrainingQueue);
        Assert.Equal(1, townCenter.TrainingProgress);
    }

    [Fact]
    public void Cancelling_a_unit_waiting_its_turn_leaves_the_training_of_the_first_one_as_it_was()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        Train.Run(match, 10);
        match.Enqueue(new CancelTrainingCommand(TestMatches.FirstPlayer, townCenter.Id, 1));

        match.Tick();

        Assert.Equal([UnitKind.Villager], townCenter.TrainingQueue);
        Assert.Equal(11, townCenter.TrainingProgress);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Cancelling_a_position_the_training_queue_does_not_have_is_rejected_and_changes_nothing(int position)
    {
        Train.AssertRejected(
            RejectionReason.NotInTrainingQueue,
            match => new CancelTrainingCommand(TestMatches.FirstPlayer, QueueOneVillager(match).Id, position));
    }

    [Fact]
    public void Cancelling_training_at_another_Players_building_is_rejected_and_changes_nothing()
    {
        Train.AssertRejected(
            RejectionReason.BuildingOfAnotherPlayer,
            match => new CancelTrainingCommand(TestMatches.SecondPlayer, QueueOneVillager(match).Id, 0));
    }

    /// <summary>Has the first Player queue one Villager at its Town Center, ticks once and returns the Town Center.</summary>
    private static BuildingState QueueOneVillager(Match match)
    {
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Tick();

        return townCenter;
    }
}
