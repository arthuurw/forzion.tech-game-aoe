using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Drives a match from the frame loop: runs the ticks a <see cref="TickClock"/> says are due
/// and tells where to draw each unit between two ticks.
/// </summary>
public sealed class MatchDriver
{
    private readonly Match match;
    private readonly TickClock clock;
    private readonly Dictionary<EntityId, MapPoint> positionsBeforeLastTick = [];

    /// <param name="match">The match to drive. From here on only <see cref="Advance"/> ticks it, or the interpolation goes wrong.</param>
    /// <param name="clock">The clock that decides when ticks are due.</param>
    public MatchDriver(Match match, TickClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);

        this.match = match;
        this.clock = clock;
    }

    /// <summary>The state of the match being driven, as of its last tick.</summary>
    public MatchState State => match.State;

    /// <summary>Queues a command for the match; the next tick <see cref="Advance"/> runs applies it.</summary>
    public void Enqueue(Command command) => match.Enqueue(command);

    /// <summary>
    /// Lets <paramref name="elapsedSeconds"/> of real time pass, running every tick that
    /// becomes due, and returns the events of all those ticks in the order they happened.
    /// A frame may run several ticks, and the match itself keeps only the last one's events.
    /// </summary>
    public IReadOnlyList<MatchEvent> Advance(double elapsedSeconds)
    {
        var due = clock.Advance(elapsedSeconds);
        var events = new List<MatchEvent>();

        for (var tick = 0; tick < due; tick++)
        {
            RememberPositions();
            match.Tick();
            events.AddRange(match.Events);
        }

        return events;
    }

    /// <summary>
    /// Where to draw <paramref name="unit"/> in the current frame: between where it stood
    /// before the last tick and where it stands now, as far along as real time has gone
    /// towards the next tick. Drawing one tick behind the simulation is what keeps the motion
    /// smooth at any frame rate.
    /// </summary>
    public MapPoint PositionOf(UnitState unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var current = ToPoint(unit.Position);

        // A unit created by the last tick has no earlier position: it is drawn where it stands.
        if (!positionsBeforeLastTick.TryGetValue(unit.Id, out var previous))
        {
            return current;
        }

        var factor = clock.InterpolationFactor;

        return new MapPoint(
            previous.X + ((current.X - previous.X) * factor),
            previous.Y + ((current.Y - previous.Y) * factor));
    }

    private void RememberPositions()
    {
        positionsBeforeLastTick.Clear();

        foreach (var unit in match.State.Units)
        {
            positionsBeforeLastTick[unit.Id] = ToPoint(unit.Position);
        }
    }

    private static MapPoint ToPoint(MapPosition position) => new(position.X.ToDouble(), position.Y.ToDouble());
}
