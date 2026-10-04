using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Drives a match from the frame loop: runs the ticks a <see cref="TickClock"/> says are due
/// and tells where to draw each unit between two ticks.
/// </summary>
public sealed class MatchDriver
{
    /// <summary>
    /// How fast, in Cells per real second, a unit slides to its place in a stack: quick enough
    /// to cross the half Cell between neighbours in a quarter of a second, slow enough to read
    /// as a move and not a jump.
    /// </summary>
    private const double StackSlideSpeed = 2;

    private readonly Match match;
    private readonly TickClock clock;
    private readonly Dictionary<EntityId, MapPoint> positionsBeforeLastTick = [];

    // A unit's drawn offset trails its place in the stack, so that it slides there.
    private readonly Dictionary<EntityId, MapPoint> stackPlaces = [];
    private Dictionary<EntityId, MapPoint> stackOffsets = [];
    private Dictionary<EntityId, MapPoint> nextStackOffsets = [];
    private int? stackPlacesTick;

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

        SlideIntoStacks(elapsedSeconds);

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
    /// apart, in rows across the Cell, so each stays visible and can be clicked. A unit slides
    /// to its place in a stack, or back from it, a little at a time as real time passes.
    /// </remarks>
    public MapPoint PositionOf(UnitState unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var current = ToPoint(unit.Position);

        // A unit created by the last tick has no earlier position: it is drawn where it stands.
        var factor = positionsBeforeLastTick.TryGetValue(unit.Id, out var previous) ? clock.InterpolationFactor : 1;
        var offset = StackOffsetOf(unit);

        return new MapPoint(
            previous.X + ((current.X - previous.X) * factor) + offset.X,
            previous.Y + ((current.Y - previous.Y) * factor) + offset.Y);
    }

    /// <summary>
    /// How far from where it stands the unit is drawn to keep it apart from the other units
    /// standing still on its Cell. A unit not drawn before is drawn in its place at once.
    /// </summary>
    private MapPoint StackOffsetOf(UnitState unit) =>
        stackOffsets.TryGetValue(unit.Id, out var offset) ? offset : StackPlaceOf(unit.Id);

    /// <summary>Where the unit belongs in the stack on its Cell; nothing for a unit alone there or walking.</summary>
    private MapPoint StackPlaceOf(EntityId unit)
    {
        // Positions change only on a tick, so the places are worked out once per tick.
        if (stackPlacesTick != match.State.Tick)
        {
            StackLayout.Lay(match.State.Units, stackPlaces);
            stackPlacesTick = match.State.Tick;
        }

        return stackPlaces.GetValueOrDefault(unit);
    }

    /// <summary>
    /// Moves every unit's drawn offset towards its place in its stack by as far as
    /// <see cref="StackSlideSpeed"/> takes it in <paramref name="elapsedSeconds"/>, and forgets
    /// the units that are gone.
    /// </summary>
    private void SlideIntoStacks(double elapsedSeconds)
    {
        var reach = StackSlideSpeed * elapsedSeconds;

        nextStackOffsets.Clear();

        foreach (var unit in match.State.Units)
        {
            var place = StackPlaceOf(unit.Id);

            nextStackOffsets[unit.Id] = stackOffsets.TryGetValue(unit.Id, out var offset) ? Towards(offset, place, reach) : place;
        }

        (stackOffsets, nextStackOffsets) = (nextStackOffsets, stackOffsets);
    }

    /// <summary>The point <paramref name="reach"/> along the way from <paramref name="from"/> to <paramref name="to"/>, or <paramref name="to"/> when nearer.</summary>
    private static MapPoint Towards(MapPoint from, MapPoint to, double reach)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));

        return distance <= reach ? to : new MapPoint(from.X + (dx * reach / distance), from.Y + (dy * reach / distance));
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
