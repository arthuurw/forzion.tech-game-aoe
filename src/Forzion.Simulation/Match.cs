namespace Forzion.Simulation;

/// <summary>
/// A running match and the single public interface of the simulation core (ADR 0001): create a
/// match from configuration, advance it one tick at a time and read its state and the events
/// of the last tick.
/// </summary>
/// <remarks>
/// The simulation is deterministic (ADR 0002): matches created from equal configurations stay
/// identical tick after tick.
/// </remarks>
public sealed class Match
{
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

    /// <summary>Advances the match by one fixed tick.</summary>
    public void Tick()
    {
        events = [];
        State.Tick++;
    }
}
