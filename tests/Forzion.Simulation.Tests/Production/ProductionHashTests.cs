using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Production;

public class ProductionHashTests
{
    [Fact]
    public void Queues_alike_but_for_how_long_their_first_unit_has_trained_have_different_hashes()
    {
        var earlier = TestMatches.TwoPlayerMatch();
        var later = TestMatches.TwoPlayerMatch();
        var townCenter = Train.TownCenter(earlier, TestMatches.FirstPlayer);
        earlier.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        earlier.Tick();
        later.Tick();
        later.Enqueue(new TrainCommand(TestMatches.FirstPlayer, townCenter.Id, UnitKind.Villager));
        later.Tick();
        earlier.Tick();

        Assert.Equal(Train.TownCenter(later, TestMatches.FirstPlayer).TrainingQueue, townCenter.TrainingQueue);
        Assert.Equal(Train.Stock(later.State.Players[0]), Train.Stock(earlier.State.Players[0]));
        Assert.NotEqual(Train.TownCenter(later, TestMatches.FirstPlayer).TrainingProgress, townCenter.TrainingProgress);
        Assert.NotEqual(later.StateHash, earlier.StateHash);
    }

    [Fact]
    public void Matches_given_the_same_training_orders_have_the_same_hash_at_every_tick()
    {
        var first = TestMatches.TwoPlayerMatch();
        var second = TestMatches.TwoPlayerMatch();

        foreach (var match in new[] { first, second })
        {
            foreach (var player in new[] { TestMatches.FirstPlayer, TestMatches.SecondPlayer })
            {
                match.Enqueue(new TrainCommand(player, Train.TownCenter(match, player).Id, UnitKind.Villager));
                match.Enqueue(new TrainCommand(player, Train.TownCenter(match, player).Id, UnitKind.Villager));
            }
        }

        for (var tick = 0; tick < 2 * Match.TrainTime(UnitKind.Villager); tick++)
        {
            first.Tick();
            second.Tick();

            Assert.Equal(first.StateHash, second.StateHash);
        }

        Assert.Equal(10, first.State.Units.Count);
    }
}
