using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiArmyTests
{
    private static readonly PlayerId Ai = TestMatches.SecondPlayer;

    [Fact]
    public void An_AI_Player_trains_soldiers_at_its_Barracks()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => AiMatches.Army(match, Ai).Any(), 8000);
    }
}
