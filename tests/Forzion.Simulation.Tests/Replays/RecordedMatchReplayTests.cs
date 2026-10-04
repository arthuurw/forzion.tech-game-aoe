using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

/// <summary>
/// A whole match of two human Players replayed from the seed and the commands recorded while it
/// was played, from the first tick to the destruction of a Town Center.
/// </summary>
public class RecordedMatchReplayTests
{
    private const ulong Seed = 2026;

    private static readonly PlayerId First = TestMatches.FirstPlayer;

    private static readonly PlayerId Second = TestMatches.SecondPlayer;

    private static MatchConfig Config() => TestMatches.TwoPlayerConfig(Seed);

    private static EntityId[] Units(params int[] ids) => [.. ids.Select(id => new EntityId(id))];

    /// <summary>
    /// The recording, with every kind of command. The first Player gathers, builds two Houses
    /// and a Barracks (the second builder joining it later), trains Villagers, soldiers of both
    /// Age I kinds and, after its Age Advance, two heavy soldiers, cancels a ranged soldier and
    /// sends its army against the second Player's Town Center (ID 23 is its own, 27 the
    /// second Player's). The second Player gathers, cancels a Villager, places a House it sends
    /// a builder to afterwards, builds a Barracks and moves the melee soldier trained there in
    /// front of its Town Center. IDs, Cells and ticks are as they were recorded.
    /// </summary>
    private static ScheduledCommand[] Commands() =>
    [
        new(0, new GatherCommand(First, Units(24, 25), new(1))),
        new(0, new TrainCommand(First, new(23), UnitKind.Villager)),
        new(0, new TrainCommand(First, new(23), UnitKind.Villager)),
        new(0, new SetRallyPointCommand(First, new(23), new(6, 7))),
        new(0, new PlaceBuildingCommand(First, BuildingKind.House, new(11, 9), Units(26))),
        new(0, new GatherCommand(Second, Units(28, 29), new(16))),
        new(0, new GatherCommand(Second, Units(30), new(15))),
        new(0, new TrainCommand(Second, new(27), UnitKind.Villager)),
        new(0, new TrainCommand(Second, new(27), UnitKind.Villager)),
        new(0, new SetRallyPointCommand(Second, new(27), new(57, 40))),
        new(5, new CancelTrainingCommand(Second, new(27), 1)),
        new(40, new PlaceBuildingCommand(First, BuildingKind.Barracks, new(12, 13), Units(25))),
        new(60, new BuildCommand(First, Units(24), new(32))),
        new(200, new PlaceBuildingCommand(Second, BuildingKind.House, new(51, 37), [])),
        new(201, new BuildCommand(Second, Units(30), new(33))),
        new(300, new GatherCommand(First, Units(34), new(6))),
        new(300, new GatherCommand(Second, Units(35), new(17))),
        new(310, new GatherCommand(First, Units(26), new(6))),
        new(400, new PlaceBuildingCommand(Second, BuildingKind.Barracks, new(50, 32), Units(28))),
        new(441, new GatherCommand(First, Units(24, 25), new(4))),
        new(441, new SetRallyPointCommand(First, new(32), new(14, 18))),
        new(460, new TrainCommand(First, new(32), UnitKind.MeleeSoldier)),
        new(480, new TrainCommand(First, new(32), UnitKind.RangedSoldier)),
        new(511, new GatherCommand(Second, Units(30), new(22))),
        new(600, new GatherCommand(First, Units(37), new(1))),
        new(722, new TrainCommand(First, new(23), UnitKind.Villager)),
        new(1022, new GatherCommand(First, Units(39), new(1))),
        new(1064, new GatherCommand(Second, Units(28), new(22))),
        new(1064, new TrainCommand(Second, new(36), UnitKind.MeleeSoldier)),
        new(1210, new TrainCommand(First, new(23), UnitKind.Villager)),
        new(1211, new PlaceBuildingCommand(First, BuildingKind.House, new(5, 14), Units(24))),
        new(1464, new MoveCommand(Second, Units(42), new(52, 36))),
        new(1510, new GatherCommand(First, Units(43), new(1))),
        new(1579, new GatherCommand(First, Units(24), new(1))),
        new(1580, new TrainCommand(First, new(32), UnitKind.MeleeSoldier)),
        new(1600, new TrainCommand(First, new(32), UnitKind.RangedSoldier)),
        new(1630, new CancelTrainingCommand(First, new(32), 1)),
        new(2635, new AgeAdvanceCommand(First, new(23))),
        new(3435, new TrainCommand(First, new(32), UnitKind.HeavySoldier)),
        new(3435, new TrainCommand(First, new(32), UnitKind.HeavySoldier)),
        new(4449, new AttackCommand(First, Units(38, 41, 44, 45, 46), new(27))),
    ];

    [Fact]
    public void The_same_seed_and_recorded_commands_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(Config(), Commands(), ExpectedEnd.Tick);
        var second = Replay.Run(Config(), Commands(), ExpectedEnd.Tick);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_recording_gives_every_kind_of_command()
    {
        Type[] kinds =
        [
            typeof(MoveCommand), typeof(GatherCommand), typeof(PlaceBuildingCommand), typeof(BuildCommand),
            typeof(TrainCommand), typeof(CancelTrainingCommand), typeof(SetRallyPointCommand),
            typeof(AttackCommand), typeof(AgeAdvanceCommand),
        ];

        Assert.Equal(
            kinds.Select(kind => kind.Name).Order(),
            Commands().Select(scheduled => scheduled.Command.GetType().Name).Distinct().Order());
    }

    [Fact]
    public void The_recorded_commands_are_all_accepted_and_the_first_Player_wins_in_Age_II_with_heavy_soldiers()
    {
        var (end, events) = Replay.RunToEnd(Config(), Commands());

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Equal([new AgeAdvanced(First, 2)], events.OfType<AgeAdvanced>());
        // The first Player's Barracks trains every soldier queued and not cancelled, the two heavy
        // soldiers last: the army the recording sends to attack.
        Assert.Equal(
            Units(38, 41, 44, 45, 46),
            events.OfType<UnitTrained>().Where(trained => trained.Building == new EntityId(32)).Select(trained => trained.Unit));
        Assert.Equal([new PlayerDefeated(Second)], events.OfType<PlayerDefeated>());
        Assert.Equal(First, end.Winner);
    }

    // The play comes from this implementation and is checked by the test above; what the
    // independent model of the hash layout (see ReplayTests), which first reproduced every value
    // recorded before, confirmed is that this is the hash of the final state as the public
    // interface shows it, and it matched the match's own hash at every tick of this replay. CI
    // runs this on Windows, Linux and macOS: every system must reach the same end. A change of
    // rules that changes how the match plays out may move its end: the tick, winner and hash
    // are recorded together, so a failure here shows all three as they now are, and the test
    // above shows whether the recording still plays the same match.
    private static readonly ReplayEnd ExpectedEnd = new(5241, First, 10263501168884646800UL);

    [Fact]
    public void A_recorded_match_reaches_its_recorded_end()
    {
        var (end, _) = Replay.RunToEnd(Config(), Commands());

        Assert.Equal(ExpectedEnd, end);
    }
}
