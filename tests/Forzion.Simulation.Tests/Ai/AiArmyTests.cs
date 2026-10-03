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

    [Fact]
    public void An_AI_Player_advances_to_Age_II_and_trains_there_the_heavy_soldier_it_unlocks()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => match.State.Players[1].Age == 2, 12000);
        AiMatches.TickUntil(match, () => AiMatches.Army(match, Ai).Any(unit => unit.Kind == UnitKind.HeavySoldier), 3000);
    }
}
