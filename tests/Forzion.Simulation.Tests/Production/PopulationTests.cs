using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class PopulationTests
{
    [Fact]
    public void A_Players_population_counts_its_units_and_the_units_in_its_training_queues()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);

        Assert.Equal(3, match.State.PopulationOf(TestMatches.FirstPlayer));

        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        match.Tick();

        Assert.Equal(4, match.State.PopulationOf(TestMatches.FirstPlayer));
        Assert.Equal(3, match.State.PopulationOf(TestMatches.SecondPlayer));

        Train.Run(match, Match.TrainTime(UnitKind.Villager));

        Assert.Equal(4, match.State.UnitsOf(TestMatches.FirstPlayer).Count());
        Assert.Equal(4, match.State.PopulationOf(TestMatches.FirstPlayer));
    }

    [Fact]
    public void Training_a_unit_that_would_take_the_population_past_its_limit_is_rejected_and_changes_nothing()
    {
        var withRejection = FullPopulation();
        var without = FullPopulation();
        var command = new TrainCommand(TestMatches.FirstPlayer, Train.TownCenter(withRejection, TestMatches.FirstPlayer).Id, UnitKind.Villager);
        withRejection.Enqueue(command);

        withRejection.Tick();
        without.Tick();

        Assert.True(withRejection.State.Players[0].AmountOf(ResourceKind.Food) >= Match.UnitCost(UnitKind.Villager).Food);
        Assert.Equal([new CommandRejected(command, RejectionReason.PopulationLimitReached)], withRejection.Events);
        Assert.Equal(without.StateHash, withRejection.StateHash);
    }

    [Fact]
    public void Cancelling_a_queued_unit_makes_room_in_the_population_for_another()
    {
        var match = FullPopulation();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);
        match.Enqueue(new CancelTrainingCommand(TestMatches.FirstPlayer, townCenter.Id, 1));
        match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));

        match.Tick();

        Assert.Empty(match.Events);
        Assert.Equal(2, townCenter.TrainingQueue.Count);
    }

    /// <summary>
    /// The default match, in which the first Player has queued Villagers at its Town Center
    /// until its population reached its limit: three starting Villagers and two in the queue.
    /// </summary>
    private static Match FullPopulation()
    {
        var match = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(match, TestMatches.FirstPlayer);

        while (match.State.PopulationOf(TestMatches.FirstPlayer) < match.State.PopulationLimitOf(TestMatches.FirstPlayer))
        {
            match.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
            match.Tick();

            Assert.Empty(match.Events);
        }

        Assert.Equal(2, townCenter.TrainingQueue.Count);

        return match;
    }
}
