using Forzion.Simulation.Tests.Ai;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

/// <summary>
/// Whole matches with AI Players, from the first tick to the end: two AI Players, and an AI
/// Player against one who does nothing. The configuration is all it takes: the AIs give their
/// own commands, so the replay has none to send.
/// </summary>
public class AiReplayTests
{
    private const ulong Seed = 2026;

    private static MatchConfig Config() => AiMatches.Config(firstIsAi: true, secondIsAi: true, Seed);

    [Fact]
    public void The_same_seed_gives_the_same_hash_at_every_tick_of_a_match_of_two_AI_Players()
    {
        var first = Replay.Run(Config(), [], ExpectedEnd.Tick);
        var second = Replay.Run(Config(), [], ExpectedEnd.Tick);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_same_seed_gives_the_same_hash_at_every_tick_of_a_match_of_an_AI_Player_against_one_who_does_nothing()
    {
        var config = AiMatches.Config(firstIsAi: false, secondIsAi: true, Seed);

        var first = Replay.Run(config, [], ExpectedEndAgainstIdlePlayer.Tick);
        var second = Replay.Run(config, [], ExpectedEndAgainstIdlePlayer.Tick);

        Assert.Equal(first, second);
    }

    // How the match of an AI Player against one who does nothing ends, from the same seed: won
    // by the AI. As with the match of two AI Players below, the play comes from this
    // implementation, and the independent model of the hash layout (see ReplayTests), which
    // first reproduced every value recorded before, gave this hash from the final state as the
    // public interface shows it and matched the match's own hash at every tick.
    private static readonly ReplayEnd ExpectedEndAgainstIdlePlayer =
        new(4563, TestMatches.SecondPlayer, 10469758762967193017UL);

    [Fact]
    public void A_recorded_match_of_an_AI_Player_against_one_who_does_nothing_reaches_its_recorded_end()
    {
        var (end, _) = Replay.RunToEnd(AiMatches.Config(firstIsAi: false, secondIsAi: true, Seed), []);

        Assert.Equal(ExpectedEndAgainstIdlePlayer, end);
    }

    // The AIs' play comes from this implementation and its behaviour is checked by the tests
    // of the AI; what the independent model of the hash layout (see ReplayTests) confirmed is
    // that this is the hash of the final state as the public interface shows it, and it
    // matched the match's own hash at every tick of this replay. CI runs this on Windows,
    // Linux and macOS: every system must reach the same hash, which is what holds the AI's
    // decisions and its draws from the match's generator to the same result everywhere. This
    // is the base for the replays of a whole match. When a Villager whose way a new building
    // blocks began to choose its job's destination again, the AIs' Villagers walked otherwise and
    // the value was recorded again: the layout stayed the same, and the model gave this one and
    // matched the match's own hash at every tick. The match still ends in the same tick.
    // When the AIs met main's rules (gatherers and builders walking to the Cell beside their
    // target with the shortest way to it, idle soldiers attacking enemy buildings, Villagers
    // waiting with their job for a way), no code that writes the hash changed, so the layout
    // stayed the same; the match now ends ten ticks sooner, still won by the second Player
    // with both in Age II, and this is its final hash.
    // When the heavy soldier began to walk 3 Cells per second instead of 2, the layout stayed
    // the same: the model reproduced the previous hash with the previous speed, gave this one and
    // matched the match's own hash at every tick. The match now ends twenty ticks later, still
    // won by the second Player with both in Age II.
    // A change of rules that changes how the AIs play may move the end of the match: the tick,
    // winner and hash are recorded together, so a failure here shows all three as they now are.
    private static readonly ReplayEnd ExpectedEnd = new(4721, TestMatches.SecondPlayer, 12206050394229018223UL);

    [Fact]
    public void A_recorded_match_of_two_AI_Players_reaches_its_recorded_end_after_both_reached_Age_II()
    {
        var (end, events) = Replay.RunToEnd(Config(), []);

        Assert.Equal(ExpectedEnd, end);
        Assert.Equal(
            [new AgeAdvanced(TestMatches.FirstPlayer, 2), new AgeAdvanced(TestMatches.SecondPlayer, 2)],
            events.OfType<AgeAdvanced>().OrderBy(advanced => advanced.Player.Value));
    }
}
