using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class MovementReplayTests
{
    private const ulong Seed = 2026;

    // Short enough to end with units still on their way, so the final hash covers paths.
    private const int Ticks = 300;

    /// <summary>
    /// Moves by both Players: across the map, onto a building, redirected halfway, to a Cell
    /// nearby and one rejected. Unit IDs are read from a match of the same configuration.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = TestMatches.TwoPlayerMatch(Seed).State;
        var first = state.UnitsOf(TestMatches.FirstPlayer).Select(unit => unit.Id).ToList();
        var second = state.UnitsOf(TestMatches.SecondPlayer).Select(unit => unit.Id).ToList();
        var firstTownCenter = state.Buildings[0].Origin;
        var secondTownCenter = state.Buildings[1].Origin;
        var nearby = state.Units.First(unit => unit.Id == second[2]).Position.Cell;

        return
        [
            new(0, new MoveCommand(TestMatches.FirstPlayer, first, new CellPosition(secondTownCenter.X + 1, secondTownCenter.Y + 1))),
            new(5, new MoveCommand(TestMatches.SecondPlayer, [second[0], second[1]], new CellPosition(0, 0))),
            new(40, new MoveCommand(TestMatches.FirstPlayer, [first[1]], new CellPosition(63, 0))),
            new(60, new MoveCommand(TestMatches.SecondPlayer, [second[0], first[0]], new CellPosition(10, 10))),
            new(100, new MoveCommand(TestMatches.SecondPlayer, [second[2]], new CellPosition(nearby.X - 3, nearby.Y))),
            new(180, new MoveCommand(TestMatches.SecondPlayer, [second[1]], firstTownCenter)),
        ];
    }

    [Fact]
    public void The_same_seed_and_moves_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);
        var second = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Moves_diverge_from_a_replay_without_them_from_the_tick_that_applies_the_first()
    {
        var withMoves = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);
        var without = Replay.Run(TestMatches.TwoPlayerConfig(Seed), [], Ticks);

        Assert.All(Enumerable.Range(0, Ticks), tick => Assert.NotEqual(without[tick], withMoves[tick]));
    }

    [Fact]
    public void A_rejected_move_does_not_change_the_hash_of_any_tick()
    {
        var commands = Commands();
        var rejected = commands.Single(scheduled => scheduled.Tick == 60);

        var withRejection = Replay.Run(TestMatches.TwoPlayerConfig(Seed), commands, Ticks);
        var without = Replay.Run(TestMatches.TwoPlayerConfig(Seed), commands.Except([rejected]).ToList(), Ticks);

        Assert.Equal(without, withRejection);
    }

    // The walks themselves come from this implementation; what an independent model of the
    // hash layout confirmed is that this is the hash of the final state as the public
    // interface shows it, paths, economy, combat and construction state included. CI runs this
    // on Windows, Linux and macOS: every system must reach the same hash, which is what holds
    // fixed-point movement and pathfinding to the same result everywhere. With starting
    // Resources, the model gave the value before with them taken out of each Player's stock,
    // and this one with them in.
    // When training joined the hash, the model reproduced the value before from the same final
    // state with the layout before, and gave this one with training state added.
    // Likewise when the rally point joined it.
    private const ulong ExpectedFinalHash = 6611298794240541393UL;

    [Fact]
    public void A_recorded_replay_of_moves_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(TestMatches.TwoPlayerConfig(Seed), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}
