using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class EconomyReplayTests
{
    private const ulong Seed = 2026;

    // Long enough for a source to run out and its Villagers to move on to the next one.
    private const int Ticks = 3000;

    /// <summary>
    /// Gathering by both Players: all three Resources, a Villager taken off gathering by a move,
    /// one sent to another Resource with a load, and a gather from a source that does not exist.
    /// Unit and source IDs are read from a match of the same configuration.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = TestMatches.TwoPlayerMatch(Seed).State;
        var first = state.Units.Where(unit => unit.Owner == TestMatches.FirstPlayer).ToList();
        var second = state.Units.Where(unit => unit.Owner == TestMatches.SecondPlayer).ToList();

        EntityId Nearest(UnitState unit, ResourceKind kind) => Gather.NearestSource(state, unit.Position.Cell, kind).Id;

        return
        [
            new(0, new GatherCommand(TestMatches.FirstPlayer, first.Select(unit => unit.Id).ToList(), Nearest(first[1], ResourceKind.Food))),
            new(0, new GatherCommand(TestMatches.SecondPlayer, [second[0].Id, second[1].Id], Nearest(second[1], ResourceKind.Wood))),
            new(300, new GatherCommand(TestMatches.SecondPlayer, [second[2].Id], Nearest(second[2], ResourceKind.Gold))),
            new(700, new MoveCommand(TestMatches.SecondPlayer, [second[0].Id], new CellPosition(32, 24))),
            new(900, new GatherCommand(TestMatches.FirstPlayer, [first[0].Id], new EntityId(100_000))),
            new(1300, new GatherCommand(TestMatches.SecondPlayer, [second[1].Id], Nearest(second[1], ResourceKind.Food))),
        ];
    }

    [Fact]
    public void The_same_seed_and_gathering_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_rejected_gather_does_not_change_the_hash_of_any_tick()
    {
        var commands = Commands();
        var rejected = commands.Single(scheduled => scheduled.Tick == 900);

        var withRejection = Replay.Run(TestMatches.TwoPlayerConfig(Seed), commands, Ticks);
        var without = Replay.Run(TestMatches.TwoPlayerConfig(Seed), commands.Except([rejected]).ToList(), Ticks);

        Assert.Equal(without, withRejection);
    }

    // The gathering itself comes from this implementation; what an independent model of the
    // hash layout confirmed is that this is the hash of the final state as the public
    // interface shows it, with the Players' Resources, one source depleted and its Villagers
    // gathering from the next, and with combat state since combat joined the hash. CI runs
    // this on Windows, Linux and macOS: every system must reach the same hash.
    private const ulong ExpectedFinalHash = 13012623972246684340UL;

    [Fact]
    public void A_recorded_replay_of_gathering_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}
