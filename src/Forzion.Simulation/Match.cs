namespace Forzion.Simulation;

/// <summary>
/// A running match and the single public interface of the simulation core (ADR 0001): create a
/// match from configuration, enqueue commands, advance it one tick at a time and read its
/// state, its state hash and the events of the last tick.
/// </summary>
/// <remarks>
/// The simulation is deterministic (ADR 0002): matches created from equal configurations and
/// given the same commands at the same ticks stay identical tick after tick.
/// </remarks>
public sealed class Match
{
    /// <summary>Ticks in one second of play. The tick is fixed: it never follows the frame rate.</summary>
    public const int TicksPerSecond = 20;

    // The systems, in the fixed order they run each tick. Order is part of the rules: changing
    // it changes the outcome of a match.
    // - Combat first: hits are struck from where units stood when the tick began, and whatever
    //   they destroy leaves the match before anything else runs, so a dead unit takes no step,
    //   gathers nothing and builds nothing in the tick it dies, and a destroyed site gets no
    //   work. The chases it starts are walked by movement in the same tick.
    // - Movement next, so the systems after it see where units stand at the end of the tick.
    // - Gathering, then construction, both after movement so they find Villagers where they
    //   arrived. A Villager does one or the other, never both, so their order between
    //   themselves only decides that a Storehouse completed in a tick takes loads from the
    //   next one on.
    // - Training after them, so a unit trained in a tick stands still until the next one, as
    //   any unit placed on the map does.
    // - Waiting after the systems that free Cells or complete drop-off points: Villagers
    //   waiting for a way choose theirs again once the tick has opened what it opens.
    // - Defeat last, after every removal of the tick: a Player whose Town Center falls is
    //   defeated, and the match ends, in the tick it falls.
    private static readonly ISystem[] Systems =
    [
        new CombatSystem(),
        new MovementSystem(),
        new GatherSystem(),
        new ConstructionSystem(),
        new TrainingSystem(),
        new WaitingSystem(),
        new DefeatSystem(),
    ];

    private readonly List<Command> pendingCommands = [];
    private IReadOnlyList<MatchEvent> events = [];

    private Match(MatchConfig config) => State = new MatchState(config);

    /// <summary>
    /// Creates a match at tick zero: generates its map from the seed and gives each Player a
    /// Town Center, starting Villagers and starting Resources.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The configuration has fewer than one or more than two Players, or a map less than 32
    /// Cells wide or high.
    /// </exception>
    public static Match Create(MatchConfig config) => new(config);

    /// <summary>What placing a building of the given kind costs, paid in full when it is placed.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Players do not place buildings of that kind.</exception>
    public static Cost BuildingCost(BuildingKind kind) =>
        Balance.Of(kind).Cost ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Players do not place this kind of building.");

    /// <summary>What training a unit of the given kind costs, paid in full when it joins a training queue.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a <see cref="UnitKind"/>.</exception>
    public static Cost UnitCost(UnitKind kind) => Balance.Of(kind).Cost;

    /// <summary>Ticks a building spends training a unit of the given kind, from when the unit heads its training queue.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a <see cref="UnitKind"/>.</exception>
    public static int TrainTime(UnitKind kind) => Balance.Of(kind).TrainTime;

    /// <summary>Side of the square footprint of a building of the given kind, in Cells.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a <see cref="BuildingKind"/>.</exception>
    public static int BuildingSize(BuildingKind kind) => Balance.Of(kind).Size;

    /// <summary>
    /// Whether a building of the given kind fits with its footprint starting on
    /// <paramref name="origin"/>: every Cell inside the map, free and with no unit standing on
    /// it. Cost is not considered. A placement that fits now may not fit by the tick that
    /// applies it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The kind is not a <see cref="BuildingKind"/>.</exception>
    public bool CanPlace(BuildingKind kind, CellPosition origin) => State.CanPlace(kind, origin);

    /// <summary>
    /// The state of the match, read-only from outside. It is the same object for the whole
    /// match and changes in place as ticks pass, so a reference kept from an earlier tick shows
    /// the current one.
    /// </summary>
    public MatchState State { get; }

    /// <summary>
    /// What happened during the last tick, in the order it happened. Each tick produces a new
    /// list, so a list read earlier is not changed by later ticks.
    /// </summary>
    public IReadOnlyList<MatchEvent> Events => events;

    /// <summary>
    /// Hash of everything in the state that influences future ticks. Two matches whose
    /// hashes differ at the same tick have diverged; comparing it is how divergence is detected.
    /// </summary>
    public ulong StateHash
    {
        get
        {
            var hasher = new StateHasher();
            State.WriteTo(hasher);

            return hasher.Hash;
        }
    }

    /// <summary>
    /// Queues a command to be applied by the next tick. Nothing changes until then: pending
    /// commands are input, not state, and are not covered by <see cref="StateHash"/>. The
    /// commands of one tick are applied in ascending <see cref="PlayerId"/> order and, for the
    /// same Player, in the order they were enqueued.
    /// </summary>
    public void Enqueue(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        pendingCommands.Add(command);
    }

    /// <summary>
    /// Advances the match by one fixed tick: applies the pending commands, then runs every
    /// system once.
    /// </summary>
    public void Tick()
    {
        var context = new TickContext(State);

        State.Tick++;
        ApplyPendingCommands(context);

        foreach (var system in Systems)
        {
            system.Run(context);
        }

        events = context.Events;
    }

    private void ApplyPendingCommands(TickContext context)
    {
        // Stable order (ADR 0002): by issuing Player, then by arrival within the Player. The
        // result does not depend on how the commands of different Players were interleaved on
        // arrival. OrderBy is a stable sort.
        foreach (var command in pendingCommands.OrderBy(command => command.Player.Value).ToList())
        {
            var issuer = State.FindPlayer(command.Player);

            if (issuer is null)
            {
                context.Reject(command, RejectionReason.UnknownPlayer);
            }
            else if (issuer.IsDefeated)
            {
                context.Reject(command, RejectionReason.DefeatedPlayer);
            }
            else
            {
                command.Execute(context, issuer);
            }
        }

        pendingCommands.Clear();
    }
}
