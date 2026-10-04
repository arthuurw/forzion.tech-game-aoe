using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiMatchTests
{
    [Fact]
    public void An_AI_Player_wins_against_a_Player_who_does_nothing()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => match.State.IsOver, TestMatches.WholeMatchLimit);

        Assert.Equal(TestMatches.SecondPlayer, match.State.Winner);
        Assert.True(match.State.Players[0].IsDefeated);
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(4UL)]
    [InlineData(5UL)]
    [InlineData(42UL)]
    public void A_match_of_two_AI_Players_ends(ulong seed)
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: true, secondIsAi: true, seed));

        AiMatches.TickUntil(match, () => match.State.IsOver, TestMatches.WholeMatchLimit);

        Assert.Single(match.State.Players, player => player.IsDefeated);
    }
}
