using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Drives a match from the frame loop: runs the ticks a <see cref="TickClock"/> says are due
/// and tells where to draw each unit between two ticks.
/// </summary>
public sealed class MatchDriver
{
    private readonly TickClock clock;
    private readonly Dictionary<EntityId, MapPoint> positionsBeforeLastTick = [];

    public MatchDriver(Match match, TickClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);

        Match = match;
        this.clock = clock;
    }

    public Match Match { get; }

    /// <summary>
    /// Lets <paramref name="elapsedSeconds"/> of real time pass, running every tick that
    /// becomes due, and returns the events of all those ticks in the order they happened.
    /// A frame may run several ticks, and <see cref="Match.Events"/> keeps only the last one's.
    /// </summary>
    public IReadOnlyList<MatchEvent> Advance(double elapsedSeconds)
    {
        var due = clock.Advance(elapsedSeconds);
        var events = new List<MatchEvent>();

        for (var tick = 0; tick < due; tick++)
        {
            RememberPositions();
            Match.Tick();
            events.AddRange(Match.Events);
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

        foreach (var unit in Match.State.Units)
        {
            positionsBeforeLastTick[unit.Id] = ToPoint(unit.Position);
        }
    }

    private static MapPoint ToPoint(MapPosition position) => new(position.X.ToDouble(), position.Y.ToDouble());
}
