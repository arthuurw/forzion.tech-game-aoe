using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class ReplayTests
{
    private const int Ticks = 200;

    private static readonly ScheduledCommand[] Commands =
    [
        new(10, new ResignCommand(new PlayerId(3))),
        new(120, new ResignCommand(TestMatches.SecondPlayer)),
        new(150, new ResignCommand(TestMatches.SecondPlayer)),
    ];

    [Fact]
    public void The_same_seed_and_commands_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands, Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands, Ticks);

        Assert.Equal(Ticks, first.Count);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Different_seeds_give_different_hashes_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands, Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2027), Commands, Ticks);

        Assert.All(Enumerable.Range(0, Ticks), tick => Assert.NotEqual(first[tick], second[tick]));
    }

    [Fact]
    public void Different_commands_diverge_from_the_tick_that_applies_them()
    {
        var withCommands = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands, Ticks);
        var without = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), [], Ticks);

        // The resignation sent at tick 120 is applied by the 121st tick, at index 120.
        Assert.Equal(without.Take(120), withCommands.Take(120));
        Assert.All(Enumerable.Range(120, Ticks - 120), tick => Assert.NotEqual(without[tick], withCommands[tick]));
    }

    [Fact]
    public void No_hash_repeats_during_a_replay()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands, Ticks);

        Assert.Equal(Ticks, hashes.Distinct().Count());
    }

    // Recorded from an independent model of the documented design, not from this
    // implementation: map generation drawn from SplitMix64, then 64-bit FNV-1a over the tick,
    // the random generator state, the map size and Cells, the Players, the last entity ID and
    // the resource sources, buildings and units in ID order, each unit followed by the number
    // of Cells in its path and those Cells, each value as eight little-endian bytes. When
    // paths joined the hash, the values were recomputed by a model of this layout that first
    // reproduced the values recorded before. When the economy joined it (each Player's Food,
    // Wood and Gold; each unit's gather source, gather phase, gather progress and load), the
    // same was done again: a model that reproduced the previous values with the previous
    // layout gave these with the new one. CI runs this on Windows, Linux and macOS: every
    // system must reach the same hash. A change that adds state to the hash changes these
    // values on purpose and must record the new ones.
    private const ulong ExpectedFinalHash = 12557458399794215910UL;

    // Seed 3 is one whose first scattering of obstacles cuts the Players apart and is drawn again.
    private const ulong ExpectedInitialHashOfSeed3 = 1678065515878766806UL;

    [Fact]
    public void A_recorded_replay_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands, Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }

    [Fact]
    public void A_new_match_has_the_recorded_hash()
    {
        Assert.Equal(ExpectedInitialHashOfSeed3, TestMatches.TwoPlayerMatch(seed: 3).StateHash);
    }
}
