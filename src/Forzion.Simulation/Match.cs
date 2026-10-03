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
    private static readonly ISystem[] Systems = [new MovementSystem(), new GatherSystem()];

    private readonly List<Command> pendingCommands = [];
    private IReadOnlyList<MatchEvent> events = [];

    private Match(MatchConfig config) => State = new MatchState(config);

    public static Match Create(MatchConfig config) => new(config);

    /// <summary>What placing a building of the given kind costs, paid in full when it is placed.</summary>
    public static Cost BuildingCost(BuildingKind kind) => Balance.BuildingCost(kind);

    /// <summary>Side of the square footprint of a building of the given kind, in Cells.</summary>
    public static int BuildingSize(BuildingKind kind) => Balance.BuildingSize(kind);

    /// <summary>
    /// Whether a building of the given kind fits with its footprint starting on
    /// <paramref name="origin"/>: every Cell inside the map, free and with no unit standing on
    /// it. Cost is not considered. A placement that fits now may not fit by the tick that
    /// applies it.
    /// </summary>
    public bool CanPlace(BuildingKind kind, CellPosition origin) => State.CanPlace(kind, origin);

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
