using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiConfigurationTests
{
    [Fact]
    public void Players_are_human_unless_configured_as_AI()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        Assert.False(match.State.Players[0].IsAi);
        Assert.True(match.State.Players[1].IsAi);
    }

    [Fact]
    public void Matches_that_differ_only_in_which_Players_are_AI_have_different_hashes()
    {
        var humans = Match.Create(TestMatches.TwoPlayerConfig());
        var withAi = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        Assert.NotEqual(humans.StateHash, withAi.StateHash);
    }
}
