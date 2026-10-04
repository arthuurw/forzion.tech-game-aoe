namespace Forzion.Presentation;

/// <summary>
/// The camera's line of sight through a point of the screen, in map coordinates: where it
/// meets the ground and how far it shifts across the map for each world unit of height above
/// the ground. One world unit is one Cell, so heights and map distances share a scale.
/// </summary>
/// <param name="Ground">Where the line meets the ground.</param>
/// <param name="ShiftPerHeight">How far the line moves across the map for each unit it rises.</param>
public readonly record struct SightLine(MapPoint Ground, MapPoint ShiftPerHeight)
{
    /// <summary>
    /// Rays this close to level count as never reaching the ground: where they would meet it
    /// lies too far out to mean anything.
    /// </summary>
    private const double LevelTolerance = 1e-4;

    /// <summary>
    /// The line of sight along a camera ray; null when the ray looks at or above the horizon
    /// and never meets the ground.
    /// </summary>
    /// <param name="origin">Where the ray starts, above the ground.</param>
    /// <param name="direction">Which way the ray points; its length does not matter.</param>
    public static SightLine? FromRay(WorldVector origin, WorldVector direction)
    {
        if (direction.Y >= -LevelTolerance)
        {
            return null;
        }

        // Along the ray the height changes by direction.Y per step: dividing by it gives the
        // step that changes the height by one unit.
        var shiftX = direction.X / direction.Y;
        var shiftZ = direction.Z / direction.Y;

        return new SightLine(
            new MapPoint(origin.X - (shiftX * origin.Y), origin.Z - (shiftZ * origin.Y)),
            new MapPoint(shiftX, shiftZ));
    }

    /// <summary>Where the line passes at <paramref name="height"/> above the ground.</summary>
    public MapPoint At(double height) =>
        new(Ground.X + (ShiftPerHeight.X * height), Ground.Y + (ShiftPerHeight.Y * height));
}
