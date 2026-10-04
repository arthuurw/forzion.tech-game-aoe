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

    /// <summary>How many units standing on one Cell are drawn side by side in a row: three rows span a Cell.</summary>
    private const int StackRowLength = 3;

    /// <summary>How fast, in Cells per real second, a unit drawn in a stack slides to its place in it: a Villager's walk.</summary>
    private const double StackSlideSpeed = 2;

    private readonly Match match;
    private readonly TickClock clock;
    private readonly Dictionary<EntityId, MapPoint> positionsBeforeLastTick = [];

    // Where each unit belongs in the stack on its Cell, and how far it has slid there.
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
    /// to its place in a stack, or back from it, at walking pace as real time passes.
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
            SpreadStacks();
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

    /// <summary>
    /// Lays out the units standing still on each Cell in rows of up to
    /// <see cref="StackRowLength"/> along the map's X, <see cref="StackSpacing"/> apart and
    /// centred on the Cell, in ascending ID order. The camera looks along the map's Y, so units
    /// side by side never hide one another; every other row is shifted half a step, so the
    /// units of a row behind show between those in front.
    /// </summary>
    private void SpreadStacks()
    {
        stackPlaces.Clear();

        var stacks = match.State.Units
            .Where(unit => !unit.IsMoving)
            .GroupBy(unit => unit.Position.Cell)
            .Where(stack => stack.Count() > 1);

        foreach (var stack in stacks)
        {
            var units = stack.ToList();
            var rows = (units.Count + StackRowLength - 1) / StackRowLength;

            for (var index = 0; index < units.Count; index++)
            {
                var row = index / StackRowLength;
                var inRow = Math.Min(StackRowLength, units.Count - (row * StackRowLength));
                var stagger = row % 2 == 1 ? StackSpacing / 2 : 0;

                stackPlaces[units[index].Id] = new MapPoint(
                    ((index % StackRowLength) - ((inRow - 1) / 2.0)) * StackSpacing + stagger,
                    (row - ((rows - 1) / 2.0)) * StackSpacing);
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
