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
    private readonly List<Command> pendingCommands = [];
    private IReadOnlyList<MatchEvent> events = [];

    private Match(MatchConfig config) => State = new MatchState(config);

    public static Match Create(MatchConfig config) => new(config);

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
    /// commands are input, not state, and are not covered by <see cref="StateHash"/>.
    /// </summary>
    public void Enqueue(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        pendingCommands.Add(command);
    }

    /// <summary>Advances the match by one fixed tick, applying the pending commands first.</summary>
    public void Tick()
    {
        var context = new TickContext(State);

        State.Tick++;
        ApplyPendingCommands(context);

        events = context.Events;
    }

    private void ApplyPendingCommands(TickContext context)
    {
        foreach (var command in pendingCommands)
        {
            if (State.FindPlayer(command.Player) is { } issuer)
            {
                command.Execute(context, issuer);
            }
        }

        pendingCommands.Clear();
    }
}
