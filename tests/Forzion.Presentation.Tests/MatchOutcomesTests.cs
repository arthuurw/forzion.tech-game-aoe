using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class MatchOutcomesTests
{
    [Fact]
    public void While_the_match_goes_on_no_Player_has_an_outcome()
    {
        var match = Match.Create(PlainConfig());
        Run(match, 10);

        Assert.Null(MatchOutcomes.For(match.State, FirstPlayer));
        Assert.Null(MatchOutcomes.For(match.State, SecondPlayer));
    }

    [Fact]
    public void Once_a_Town_Center_falls_its_Player_is_defeated_and_the_other_is_victorious()
    {
        var match = Siege(FirstPlayer);

        RunUntilOver(match);

        Assert.Equal(MatchOutcome.Victory, MatchOutcomes.For(match.State, FirstPlayer));
        Assert.Equal(MatchOutcome.Defeat, MatchOutcomes.For(match.State, SecondPlayer));
    }

    [Fact]
    public void A_match_that_ends_without_a_winner_is_a_defeat_for_every_Player()
    {
        var match = Siege(FirstPlayer, SecondPlayer);

        RunUntilOver(match);

        Assert.Null(match.State.Winner);
        Assert.Equal(MatchOutcome.Defeat, MatchOutcomes.For(match.State, FirstPlayer));
        Assert.Equal(MatchOutcome.Defeat, MatchOutcomes.For(match.State, SecondPlayer));
    }

    private static void RunUntilOver(Match match)
    {
        for (var tick = 0; tick < 10_000 && !match.State.IsOver; tick++)
        {
            match.Tick();
        }

        Assert.True(match.State.IsOver);
    }
}
