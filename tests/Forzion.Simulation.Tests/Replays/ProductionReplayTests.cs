using Forzion.Simulation.Tests.Construction;
using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Replays;

public class ProductionReplayTests
{
    private const ulong Seed = 2026;

    // Long enough for every unit queued to be trained and to reach its rally point.
    private const int Ticks = 2000;

    private static MatchConfig Config() => TestMatches.TwoPlayerConfig(Seed);

    /// <summary>
    /// Production by both Players: Villagers trained at the Town Centers with rally points, one
    /// of them cancelled; a Barracks and a House built by the first Player, an order to train at
    /// the Barracks while it is still a site, and melee and ranged soldiers trained there with a
    /// rally point, the ranged one cancelled, between them an order for a heavy soldier, which
    /// Age I has not unlocked. Unit, building and source IDs and the origins are read from a
    /// match of the same configuration.
    /// </summary>
    private static ScheduledCommand[] Commands()
    {
        var state = Match.Create(Config()).State;
        var first = TestMatches.FirstPlayer;
        var second = TestMatches.SecondPlayer;
        var villagers = state.UnitsOf(first).Select(unit => unit.Id).ToList();
        var firstTownCenter = state.Buildings[0];
        var secondTownCenter = state.Buildings[1];
        var wood = Gather.NearestSource(state, state.Units[1].Position.Cell, ResourceKind.Wood);
        var barracks = Site.FreeOriginNear(state, firstTownCenter.Origin, Match.BuildingSize(BuildingKind.Barracks));
        var house = Site.FreeOriginNear(state, wood.Cell, Match.BuildingSize(BuildingKind.House));

        // Entities take IDs in creation order: the sites are placed before any unit is trained.
        var barracksId = new EntityId(state.Units[^1].Id.Value + 1);

        return
        [
            new(0, new PlaceBuildingCommand(first, BuildingKind.Barracks, barracks, [villagers[0], villagers[1]])),
            new(0, new PlaceBuildingCommand(first, BuildingKind.House, house, [villagers[2]])),
            new(0, new SetRallyPointCommand(first, firstTownCenter.Id, new CellPosition(wood.Cell.X, wood.Cell.Y + 1))),
            new(0, new TrainCommand(first, firstTownCenter.Id, UnitKind.Villager)),
            new(0, new TrainCommand(second, secondTownCenter.Id, UnitKind.Villager)),
            new(0, new TrainCommand(second, secondTownCenter.Id, UnitKind.Villager)),
            new(5, new SetRallyPointCommand(second, secondTownCenter.Id, new CellPosition(32, 24))),
            new(10, new CancelTrainingCommand(second, secondTownCenter.Id, 1)),
            new(20, new TrainCommand(first, barracksId, UnitKind.MeleeSoldier)),
            new(700, new SetRallyPointCommand(first, barracksId, new CellPosition(32, 24))),
            new(700, new TrainCommand(first, barracksId, UnitKind.MeleeSoldier)),
            new(700, new TrainCommand(first, barracksId, UnitKind.HeavySoldier)),
            new(700, new TrainCommand(first, barracksId, UnitKind.RangedSoldier)),
            new(710, new CancelTrainingCommand(first, barracksId, 1)),
        ];
    }

    [Fact]
    public void The_same_seed_and_production_give_the_same_hash_at_every_tick()
    {
        var first = Replay.Run(Config(), Commands(), Ticks);
        var second = Replay.Run(Config(), Commands(), Ticks);

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_replay_trains_every_unit_not_cancelled_and_rejects_only_the_orders_for_the_site_and_the_locked_unit()
    {
        var match = Match.Create(Config());
        var commands = Commands();
        var rejected = new List<CommandRejected>();
        var trained = new List<UnitKind>();

        for (var tick = 0; tick < Ticks; tick++)
        {
            foreach (var scheduled in commands.Where(scheduled => scheduled.Tick == tick))
            {
                match.Enqueue(scheduled.Command);
            }

            match.Tick();
            rejected.AddRange(match.Events.OfType<CommandRejected>());
            trained.AddRange(match.Events.OfType<UnitTrained>().Select(done => match.State.Units.Single(unit => unit.Id == done.Unit).Kind));
        }

        var heavyOrder = commands.Single(scheduled => scheduled.Command is TrainCommand { Kind: UnitKind.HeavySoldier }).Command;
        Assert.Equal(
            [
                new CommandRejected(commands.Single(scheduled => scheduled.Tick == 20).Command, RejectionReason.BuildingNotComplete),
                new CommandRejected(heavyOrder, RejectionReason.UnitLocked),
            ],
            rejected);
        Assert.Equal([UnitKind.Villager, UnitKind.Villager, UnitKind.MeleeSoldier], trained.Order());
        Assert.All(match.State.Buildings, building => Assert.Empty(building.TrainingQueue));
        Assert.All(match.State.Units, unit => Assert.False(unit.IsMoving));
    }

    [Fact]
    public void A_rejected_training_order_does_not_change_the_hash_of_any_tick()
    {
        var commands = Commands();
        var rejected = commands.Single(scheduled => scheduled.Tick == 20);

        var withRejection = Replay.Run(Config(), commands, Ticks);
        // Not Except, which would also drop one of the two equal orders to train a Villager.
        var without = Replay.Run(Config(), commands.Where(scheduled => scheduled != rejected).ToList(), Ticks);

        Assert.Equal(without, withRejection);
    }

    // The production itself comes from this implementation and is checked by the test above;
    // what an independent model of the hash layout (see ReplayTests) confirmed is that this is
    // the hash of the final state as the public interface shows it, and it matched the
    // match's own hash at every tick of this replay, while queues held units of several kinds
    // and buildings had rally points. CI runs this on Windows, Linux and macOS: every system
    // must reach the same hash. When the heavy soldier was locked behind Age II, its order
    // became a rejection and the cancel that followed it moved to the ranged soldier's new
    // place in the queue; the layout stayed the same, and the model, which reproduced every
    // value recorded in the other replays, gave this one and matched the match's own hash at
    // every tick. When the Ages joined the hash (each Player's Age and its Faction's data, and
    // each building's Age Advance underway), the model reproduced the value before from the
    // same final state with the layout before, gave this one with the Ages added and matched
    // the match's own hash at every tick.
    private const ulong ExpectedFinalHash = 11357276573190627401UL;

    [Fact]
    public void A_recorded_replay_of_production_reaches_the_recorded_final_hash()
    {
        var hashes = Replay.Run(Config(), Commands(), Ticks);

        Assert.Equal(ExpectedFinalHash, hashes[^1]);
    }
}
