using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Ai;

public class AiMatchTests
{
    // Twenty minutes of play: far more than either match below takes.
    private const int Limit = 20 * 60 * Match.TicksPerSecond;

    [Fact]
    public void An_AI_Player_wins_against_a_Player_who_does_nothing()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true));

        AiMatches.TickUntil(match, () => match.State.IsOver, Limit);

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

        AiMatches.TickUntil(match, () => match.State.IsOver, Limit);

        Assert.Single(match.State.Players, player => player.IsDefeated);
    }
}
