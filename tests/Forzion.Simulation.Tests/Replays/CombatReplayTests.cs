using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class CombatReplayTests
{
    private const ulong Seed = 2026;

    // Long enough for the first Player to destroy the second's Town Center, ending the match,
    // and for its ranged soldier to strike down the defender afterwards.
    private const int Ticks = 600;

    /// <summary>
    /// The first Player starts with melee soldiers on the flanks of the second Player's Town
    /// Center and a ranged soldier at home; the second Player with a melee soldier on a corner
    /// of its own Town Center, close enough to the attackers to react on its own.
    /// </summary>
    private static MatchConfig Config()
    {
        var plain = TestMatches.TwoPlayerMatch(Seed);
        var second = TestMatches.SecondPlayer;
        IEnumerable<(int X, int Y)> flanks = [(-2, -1), (-2, 0), (-2, 1), (2, -1), (2, 0), (2, 1)];

        return TestArmies.Config(
            Seed,
            first:
            [
                .. flanks.Select(flank => new StartingUnit(
                    UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, second, flank.X, flank.Y))),
                new(UnitKind.RangedSoldier, TestArmies.BesideHome(plain, TestMatches.FirstPlayer, 2, 0)),
            ],
            second: [new(UnitKind.MeleeSoldier, TestArmies.BesideHome(plain, second, -2, -2))]);
    }

    /// <summary>
    /// The first Player's melee soldiers attack the second Player's Town Center and its ranged
    /// soldier crosses the map after the defender; then the second Player's Villagers flee.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = Match.Create(Config()).State;
        var first = TestMatches.FirstPlayer;
        var second = TestMatches.SecondPlayer;
        var melee = state.Units.Where(unit => unit.Owner == first && unit.Kind == UnitKind.MeleeSoldier).Select(unit => unit.Id).ToList();
        var archer = state.Units.Single(unit => unit.Kind == UnitKind.RangedSoldier).Id;
        var defender = state.Units.Single(unit => unit.Owner == second && unit.Kind == UnitKind.MeleeSoldier).Id;
        var villagers = state.Units.Where(unit => unit.Owner == second && unit.Kind == UnitKind.Villager).Select(unit => unit.Id).ToList();
        var townCenter = state.Buildings.Single(building => building.Owner == second).Id;

        return
        [
            new(0, new AttackCommand(first, melee, townCenter)),
            new(0, new AttackCommand(first, [archer], defender)),
            new(30, new MoveCommand(second, villagers, new CellPosition(32, 24))),
        ];
    }

    [Fact]
    public void The_same_seed_and_commands_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(Config(), Commands(), Ticks);
        var second = Replay.Run(Config(), Commands(), Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_replay_ends_with_the_first_Player_winning()
    {
        var match = Match.Create(Config());
        var commands = Commands();

        for (var tick = 0; tick < Ticks; tick++)
        {
            foreach (var scheduled in commands.Where(scheduled => scheduled.Tick == tick))
            {
                match.Enqueue(scheduled.Command);
            }

            match.Tick();
        }

        Assert.True(match.State.IsOver);
        Assert.Equal(TestMatches.FirstPlayer, match.State.Winner);
    }

    // The fight itself comes from this implementation; an independent model of the hash layout
    // (see ReplayTests) confirmed this is the hash of the final state, and matched the match's
    // own hash at every tick. CI runs this on Windows, Linux and macOS: every system must reach
    // the same hash, which is what holds combat (ranges, chases, hits, deaths and the end of
    // the match) to the same result everywhere.
    private const ulong ExpectedFinalHash = 3355738942257518089UL;

    [Fact]
    public void A_recorded_replay_of_combat_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(Config(), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}
