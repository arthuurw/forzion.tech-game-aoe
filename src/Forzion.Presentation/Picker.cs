using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Finds what a line of sight points at. A unit stands as an upright cylinder on its drawn
/// position; a building or a resource source as a box over its Cells. The line picks a shape
/// where it passes through it.
/// </summary>
internal sealed class Picker(MatchDriver driver, PickSizes sizes)
{
    /// <summary>
    /// The entity the line points at: a <see cref="UnitState"/>, a <see cref="BuildingState"/>,
    /// a <see cref="ResourceSourceState"/> or null for bare ground. Units come first, since
    /// they are small and stand in front of what they are next to.
    /// </summary>
    public object? At(SightLine sight) => (object?)UnitAt(sight) ?? BuildingOrSourceAt(sight);

    /// <summary>
    /// The units seen inside the area that four lines of sight, through the corners of a box
    /// on screen taken in turn around it, mark out. A unit counts as inside when the middle of
    /// its body is, half its height up.
    /// </summary>
    public IEnumerable<UnitState> UnitsInside(IReadOnlyList<SightLine> corners)
    {
        var middle = sizes.UnitHeight / 2;
        var area = corners.Select(corner => corner.At(middle)).ToList();

        return driver.Match.State.Units.Where(unit => IsInside(driver.PositionOf(unit), area));
    }

    /// <summary>The unit the line points at, the one nearest to the line when several are.</summary>
    private UnitState? UnitAt(SightLine sight)
    {
        var bottom = sight.At(0);
        var top = sight.At(sizes.UnitHeight);
        UnitState? nearest = null;
        var nearestDistance = sizes.UnitRadius;

        // Strictly nearer only: on a tie the unit with the lower ID, met first, keeps the pick.
        foreach (var unit in driver.Match.State.Units)
        {
            var distance = DistanceToSegment(driver.PositionOf(unit), bottom, top);

            if (distance < nearestDistance || (nearest is null && distance <= nearestDistance))
            {
                nearest = unit;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <summary>
    /// The building or resource source the line meets first coming down from the camera.
    /// Footprints never overlap, so on a tie the first in the state's order is as good as any.
    /// </summary>
    private object? BuildingOrSourceAt(SightLine sight)
    {
        var top = sight.At(sizes.BuildingAndSourceHeight);
        var bottom = sight.At(0);
        object? first = null;
        var firstEntry = double.PositiveInfinity;

        void Consider(object entity, CellPosition origin, int width, int height)
        {
            var entry = EntryAlong(top, bottom, origin.X, origin.Y, origin.X + width, origin.Y + height);

            if (entry < firstEntry)
            {
                first = entity;
                firstEntry = entry;
            }
        }

        foreach (var building in driver.Match.State.Buildings)
        {
            Consider(building, building.Origin, building.Width, building.Height);
        }

        foreach (var source in driver.Match.State.ResourceSources)
        {
            Consider(source, source.Cell, 1, 1);
        }

        return first;
    }

    /// <summary>
    /// How far along the segment from <paramref name="start"/> to <paramref name="end"/>, from 0
    /// to 1, it enters the rectangle; infinity when it misses it (Liang-Barsky clipping).
    /// </summary>
    private static double EntryAlong(MapPoint start, MapPoint end, double minX, double minY, double maxX, double maxY)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var entry = 0.0;
        var exit = 1.0;

        // Each pair is how fast the segment approaches one side of the rectangle and how far
        // inside that side its start lies.
        (double Rate, double Room)[] sides =
        [
            (-dx, start.X - minX),
            (dx, maxX - start.X),
            (-dy, start.Y - minY),
            (dy, maxY - start.Y),
        ];

        foreach (var (rate, room) in sides)
        {
            if (rate == 0)
            {
                if (room < 0)
                {
                    return double.PositiveInfinity;
                }

                continue;
            }

            var crossing = room / rate;

            if (rate < 0)
            {
                entry = Math.Max(entry, crossing);
            }
            else
            {
                exit = Math.Min(exit, crossing);
            }
        }

        return entry <= exit ? entry : double.PositiveInfinity;
    }

    /// <summary>
    /// Whether the point lies inside the convex polygon, or on its edge, whichever way round
    /// its corners are given: the point is on the same side of every edge.
    /// </summary>
    private static bool IsInside(MapPoint point, List<MapPoint> polygon)
    {
        var left = false;
        var right = false;

        for (var index = 0; index < polygon.Count; index++)
        {
            var start = polygon[index];
            var end = polygon[(index + 1) % polygon.Count];
            var side = ((end.X - start.X) * (point.Y - start.Y)) - ((end.Y - start.Y) * (point.X - start.X));

            left |= side > 0;
            right |= side < 0;
        }

        return !(left && right);
    }

    private static double DistanceToSegment(MapPoint point, MapPoint start, MapPoint end)
    {
        var alongX = end.X - start.X;
        var alongY = end.Y - start.Y;
        var lengthSquared = (alongX * alongX) + (alongY * alongY);

        // A vertical line of sight has no length across the map: the distance is to its foot.
        var fraction = lengthSquared == 0
            ? 0
            : Math.Clamp((((point.X - start.X) * alongX) + ((point.Y - start.Y) * alongY)) / lengthSquared, 0, 1);

        var dx = point.X - (start.X + (alongX * fraction));
        var dy = point.Y - (start.Y + (alongY * fraction));

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
