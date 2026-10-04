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

    // The tick the match ends in: the replay runs exactly to the end.
    private const int Ticks = 4721;

    // The tick the match of an AI Player against one who does nothing ends in, from the same seed.
    private const int TicksAgainstIdlePlayer = 4563;

    private static MatchConfig Config() => AiMatches.Config(firstIsAi: true, secondIsAi: true, Seed);

    [Fact]
    public void The_same_seed_gives_the_same_hash_at_every_tick_of_a_match_of_two_AI_Players()
    {
        var first = Replay.Run(Config(), [], Ticks);
        var second = Replay.Run(Config(), [], Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_same_seed_gives_the_same_hash_at_every_tick_of_a_match_of_an_AI_Player_against_one_who_does_nothing()
    {
        var config = AiMatches.Config(firstIsAi: false, secondIsAi: true, Seed);

        var first = Replay.Run(config, [], TicksAgainstIdlePlayer);
        var second = Replay.Run(config, [], TicksAgainstIdlePlayer);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_match_of_an_AI_Player_against_one_who_does_nothing_ends_in_its_last_tick_won_by_the_AI()
    {
        var match = Match.Create(AiMatches.Config(firstIsAi: false, secondIsAi: true, Seed));

        var ticks = AiMatches.TickUntil(match, () => match.State.IsOver, TicksAgainstIdlePlayer);

        Assert.Equal(TicksAgainstIdlePlayer, ticks);
        Assert.Equal(TestMatches.SecondPlayer, match.State.Winner);
    }

    [Fact]
    public void The_match_of_two_AI_Players_ends_in_its_last_tick_after_both_reached_Age_II()
    {
        var match = Match.Create(Config());

        var ticks = AiMatches.TickUntil(match, () => match.State.IsOver, Ticks);

        Assert.Equal(Ticks, ticks);
        Assert.Equal(TestMatches.SecondPlayer, match.State.Winner);
        Assert.All(match.State.Players, player => Assert.Equal(2, player.Age));
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
    private const ulong ExpectedFinalHash = 12206050394229018223UL;

    [Fact]
    public void A_recorded_match_of_two_AI_Players_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(Config(), [], Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}
