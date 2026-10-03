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
}
