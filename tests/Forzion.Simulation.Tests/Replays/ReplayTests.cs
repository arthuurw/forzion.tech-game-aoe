using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class ReplayTests
{
    private const int Ticks = 200;

    /// <summary>
    /// A command from a Player not in the match, rejected, then two moves by the second Player.
    /// Unit IDs are read from a match of the same configuration.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = TestMatches.TwoPlayerMatch(seed: 2026).State;
        var second = state.Units.Where(unit => unit.Owner == TestMatches.SecondPlayer).Select(unit => unit.Id).ToList();

        return
        [
            new(10, new MoveCommand(new PlayerId(3), second, new CellPosition(20, 20))),
            new(120, new MoveCommand(TestMatches.SecondPlayer, second, new CellPosition(30, 20))),
            new(150, new MoveCommand(TestMatches.SecondPlayer, [second[0]], new CellPosition(40, 30))),
        ];
    }

    [Fact]
    public void The_same_seed_and_commands_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands(), Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands(), Ticks);

        Assert.Equal(Ticks, first.Count);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Different_seeds_give_different_hashes_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands(), Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2027), Commands(), Ticks);

        Assert.All(Enumerable.Range(0, Ticks), tick => Assert.NotEqual(first[tick], second[tick]));
    }

    [Fact]
    public void Different_commands_diverge_from_the_tick_that_applies_them()
    {
        var withCommands = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands(), Ticks);
        var without = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), [], Ticks);

        // The move sent at tick 120 is applied by the 121st tick, at index 120.
        Assert.Equal(without.Take(120), withCommands.Take(120));
        Assert.All(Enumerable.Range(120, Ticks - 120), tick => Assert.NotEqual(without[tick], withCommands[tick]));
    }

    [Fact]
    public void No_hash_repeats_during_a_replay()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands(), Ticks);

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
    // layout gave these with the new one. Combat then added each building's hit points, each
    // unit's hit points, target (0 for none) and attack progress after its economy state, and
    // whether the match is over and its winner (0 for none) at the end; the values were
    // recomputed the same way, by a model that reproduced the previous values with the
    // previous layout and matched the match's own hash at every tick of the replays.
    // Construction then added each building's build progress after its hit points and each
    // unit's construction site (0 for none) after its combat state; once more a model that
    // reproduced the previous values from the same states with the layout before gave these
    // and matched the match's own hash at every tick of the replays. CI runs this on Windows,
    // Linux and macOS: every system must reach the same hash. A change that adds state to the
    // hash changes these values on purpose and must record the new ones. When Players began
    // the match with starting Resources, the layout stayed the same: the model gave the value
    // before from the new final state with the starting Resources taken out of each Player's
    // stock, and this one with them in, and matched the match's own hash at every tick.
    // Training then added each building's training queue (its length, then the kind of each unit in
    // order) and its training progress after its build progress; once more the model reproduced the
    // value before from the same final state with the layout before, gave this one and matched the
    // match's own hash at every tick of the replays.
    // The rally point then followed the training progress, as whether the building has one and its
    // Cell ((0, 0) for none); the model again reproduced the value before with the layout before,
    // gave this one and matched the match's own hash at every tick of the replays.
    private const ulong ExpectedFinalHash = 16204813214023181089UL;

    // Seed 3 is one whose first scattering of obstacles cuts the Players apart and is drawn again.
    // When walled-in sources began to be dropped, the value was recomputed by the same model of
    // the layout, which first reproduced the value recorded before from the state of the old
    // generator; the two states differ only by one walled-in source pair, freed. With combat
    // in the hash, and again with construction, the value was recomputed by the model, which
    // reproduced the value before with the layout before. With starting Resources, the model
    // gave the value before with them taken out of each Player's stock, and this one with them in.
    // With training in the hash, the model reproduced the value before with the layout before and
    // gave this one.
    // Likewise with the rally point.
    private const ulong ExpectedInitialHashOfSeed3 = 14288579479582643681UL;

    [Fact]
    public void A_recorded_replay_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(seed: 2026), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }

    [Fact]
    public void A_new_match_has_the_recorded_hash()
    {
        Assert.Equal(ExpectedInitialHashOfSeed3, TestMatches.TwoPlayerMatch(seed: 3).StateHash);
    }
}
