using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class AgeReplayTests
{
    private const ulong Seed = 2026;

    // Long enough for both Age Advances, the Barracks and the heavy soldier trained there.
    private const int Ticks = 3200;

    private static MatchConfig Config() => TestMatches.TwoPlayerConfig(Seed);

    /// <summary>
    /// Age Advances by both Players: Food gathered for them, the first Player's first Age
    /// Advance refused for want of Resources, both Players
    /// advancing to Age II a hundred ticks apart, then the first Player gathering Gold, building a
    /// Barracks and training there the heavy soldier Age II unlocks. Unit, source and building
    /// IDs and the origins are read from a match of the same configuration.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = Match.Create(Config()).State;
        var first = TestMatches.FirstPlayer;
        var second = TestMatches.SecondPlayer;
        var firstVillagers = state.UnitsOf(first).Select(unit => unit.Id).ToList();
        var secondVillagers = state.UnitsOf(second).Select(unit => unit.Id).ToList();
        var firstTownCenter = state.Buildings[0];
        var secondTownCenter = state.Buildings[1];
        var firstFood = Gather.NearestSource(state, state.Units[1].Position.Cell, ResourceKind.Food);
        var firstGold = Gather.NearestSource(state, state.Units[1].Position.Cell, ResourceKind.Gold);
        var secondFood = Gather.NearestSource(state, state.UnitsOf(second).ElementAt(1).Position.Cell, ResourceKind.Food);
        var barracks = Site.FreeOriginNear(state, firstTownCenter.Origin, Match.BuildingSize(BuildingKind.Barracks));

        // Entities take IDs in creation order: the Barracks is the first entity created in the match.
        var barracksId = new EntityId(state.Units[^1].Id.Value + 1);

        return
        [
            new(0, new GatherCommand(first, firstVillagers, firstFood.Id)),
            new(0, new GatherCommand(second, secondVillagers, secondFood.Id)),
            new(10, new AgeAdvanceCommand(first, firstTownCenter.Id)),
            new(1000, new AgeAdvanceCommand(first, firstTownCenter.Id)),
            new(1000, new GatherCommand(first, firstVillagers, firstGold.Id)),
            new(1100, new AgeAdvanceCommand(second, secondTownCenter.Id)),
            new(1900, new PlaceBuildingCommand(first, BuildingKind.Barracks, barracks, [firstVillagers[0], firstVillagers[1]])),
            new(2500, new TrainCommand(first, barracksId, UnitKind.HeavySoldier)),
        ];
    }

    [Fact]
    public void The_same_seed_and_Age_Advances_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(Config(), Commands(), Ticks);
        var second = Replay.Run(Config(), Commands(), Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_replay_advances_both_Players_to_Age_II_and_trains_the_heavy_soldier_there()
    {
        var match = Match.Create(Config());
        var commands = Commands();
        var rejected = new List<CommandRejected>();
        var advanced = new List<(int Tick, AgeAdvanced Event)>();
        var trained = new List<UnitKind>();

        for (var tick = 0; tick < Ticks; tick++)
        {
            foreach (var scheduled in commands.Where(scheduled => scheduled.Tick == tick))
            {
                match.Enqueue(scheduled.Command);
            }

            match.Tick();
            rejected.AddRange(match.Events.OfType<CommandRejected>());
            advanced.AddRange(match.Events.OfType<AgeAdvanced>().Select(done => (match.State.Tick, done)));
            trained.AddRange(match.Events.OfType<UnitTrained>().Select(done => match.State.Units.Single(unit => unit.Id == done.Unit).Kind));
        }

        var advanceTime = Factions.Portuguese.Ages[1].AdvanceTime;
        Assert.Equal([new CommandRejected(commands.Single(scheduled => scheduled.Tick == 10).Command, RejectionReason.NotEnoughResources)], rejected);
        Assert.Equal(
            [
                (1000 + advanceTime, new AgeAdvanced(TestMatches.FirstPlayer, 2)),
                (1100 + advanceTime, new AgeAdvanced(TestMatches.SecondPlayer, 2)),
            ],
            advanced);
        Assert.Equal([UnitKind.HeavySoldier], trained);
        Assert.All(match.State.Players, player => Assert.Equal(2, player.Age));
    }

    [Fact]
    public void A_rejected_Age_Advance_does_not_change_the_hash_of_any_tick()
    {
        var commands = Commands();
        var rejected = commands.Single(scheduled => scheduled.Tick == 10);

        var withRejection = Replay.Run(Config(), commands, Ticks);
        var without = Replay.Run(Config(), commands.Where(scheduled => scheduled != rejected).ToList(), Ticks);

        Assert.Equal(without, withRejection);
    }

    // The Age Advances themselves come from this implementation and are checked by the test
    // above; what an independent model of the hash layout (see ReplayTests) confirmed is that
    // this is the hash of the final state as the public interface shows it, and it matched the
    // match's own hash at every tick of this replay, while advances were underway and after
    // the Players reached Age II. CI runs this on Windows, Linux and macOS: every system must
    // reach the same hash.
    private const ulong ExpectedFinalHash = 1025558472475379430UL;

    [Fact]
    public void A_recorded_replay_of_Age_Advances_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(Config(), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}
