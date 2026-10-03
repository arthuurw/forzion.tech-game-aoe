using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiEconomyTests
{
    private static readonly PlayerId Human = TestMatches.FirstPlayer;
    private static readonly PlayerId Ai = TestMatches.SecondPlayer;

    [Fact]
    public void An_AI_Player_sends_its_idle_Villagers_to_gather_in_the_first_tick()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        match.Tick();

        Assert.All(match.State.UnitsOf(Ai), villager => Assert.NotNull(villager.GatherSource));
        Assert.All(match.State.UnitsOf(Human), villager => Assert.Null(villager.GatherSource));
    }

    [Fact]
    public void An_AI_Player_trains_a_Villager_at_its_Town_Center_paying_its_cost()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));
        var food = match.State.Players[1].AmountOf(ResourceKind.Food);

        match.Tick();

        Assert.Equal([UnitKind.Villager], AiMatches.TownCenter(match, Ai).TrainingQueue);
        Assert.Equal(food - Match.UnitCost(UnitKind.Villager).Food, match.State.Players[1].AmountOf(ResourceKind.Food));
        Assert.Empty(AiMatches.TownCenter(match, Human).TrainingQueue);
    }

    [Fact]
    public void An_AI_Player_trains_Villagers_one_after_another_never_queueing_more_than_one()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));
        var townCenter = AiMatches.TownCenter(match, Ai);
        var trained = 0;

        for (var tick = 0; tick < (2 * Match.TrainTime(UnitKind.Villager)) + 1; tick++)
        {
            match.Tick();
            trained += match.Events.OfType<UnitTrained>().Count(done => done.Building == townCenter.Id);
            Assert.True(townCenter.TrainingQueue.Count <= 1);
        }

        Assert.Equal(2, trained);
    }
}
