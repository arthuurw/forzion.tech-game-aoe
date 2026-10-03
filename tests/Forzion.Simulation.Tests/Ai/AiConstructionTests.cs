using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiConstructionTests
{
    private static readonly PlayerId Ai = TestMatches.SecondPlayer;

    [Fact]
    public void An_AI_Player_builds_a_House_before_its_population_reaches_the_limit()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => AiMatches.Buildings(match, Ai, BuildingKind.House).Any(house => house.IsComplete), 1500);

        Assert.True(match.State.PopulationLimitOf(Ai) > match.State.PopulationOf(Ai));
    }

    [Fact]
    public void An_AI_Player_builds_a_single_Barracks()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => AiMatches.Buildings(match, Ai, BuildingKind.Barracks).Any(barracks => barracks.IsComplete), 5000);
        AiMatches.Run(match, 200);

        Assert.Single(AiMatches.Buildings(match, Ai, BuildingKind.Barracks));
    }
}
