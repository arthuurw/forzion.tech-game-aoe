using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Drives a match from the frame loop: runs the ticks a <see cref="TickClock"/> says are due
/// and tells where to draw each unit between two ticks.
/// </summary>
public sealed class MatchDriver
{
    /// <summary>How far apart, in Cells, units standing on one Cell are drawn: as wide as two of their placeholders.</summary>
    private const double StackSpacing = 0.5;

    private readonly Match match;
    private readonly TickClock clock;
    private readonly Dictionary<EntityId, MapPoint> positionsBeforeLastTick = [];
    private readonly Dictionary<EntityId, MapPoint> stackOffsets = [];
    private int? stackOffsetsTick;

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

    /// <summary>Whether the match would place a building of the given kind there now, as <see cref="Match.CanPlace"/> says.</summary>
    public bool CanPlace(BuildingKind kind, CellPosition origin) => match.CanPlace(kind, origin);

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
    /// <remarks>
    /// Units do not block one another, so several may stand still on one Cell. Those are drawn
    /// apart, on a ring around the Cell's centre, so each stays visible and can be clicked.
    /// </remarks>
    public MapPoint PositionOf(UnitState unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var current = ToPoint(unit.Position);
        var offset = StackOffsetOf(unit);

        // A unit created by the last tick has no earlier position: it is drawn where it stands.
        if (!positionsBeforeLastTick.TryGetValue(unit.Id, out var previous))
        {
            return new MapPoint(current.X + offset.X, current.Y + offset.Y);
        }

        var factor = clock.InterpolationFactor;

        return new MapPoint(
            previous.X + ((current.X - previous.X) * factor) + offset.X,
            previous.Y + ((current.Y - previous.Y) * factor) + offset.Y);
    }

    /// <summary>
    /// How far from where it stands the unit is drawn to keep it apart from the other units
    /// standing still on its Cell; nothing for a unit alone there or walking.
    /// </summary>
    private MapPoint StackOffsetOf(UnitState unit)
    {
        // Positions change only on a tick, so the offsets are worked out once per tick.
        if (stackOffsetsTick != match.State.Tick)
        {
            SpreadStacks();
            stackOffsetsTick = match.State.Tick;
        }

        return stackOffsets.GetValueOrDefault(unit.Id);
    }

    /// <summary>
    /// Places the units standing still on each Cell evenly on a ring around its centre, in
    /// ascending ID order, the ring just wide enough to keep neighbours
    /// <see cref="StackSpacing"/> apart.
    /// </summary>
    private void SpreadStacks()
    {
        stackOffsets.Clear();

        var stacks = match.State.Units
            .Where(unit => !unit.IsMoving)
            .GroupBy(unit => unit.Position.Cell)
            .Where(stack => stack.Count() > 1);

        foreach (var stack in stacks)
        {
            var units = stack.ToList();
            var step = 2 * Math.PI / units.Count;
            var radius = StackSpacing / 2 / Math.Sin(step / 2);

            for (var index = 0; index < units.Count; index++)
            {
                stackOffsets[units[index].Id] = new MapPoint(radius * Math.Cos(step * index), radius * Math.Sin(step * index));
            }
        }
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
