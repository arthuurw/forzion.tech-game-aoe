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
    /// <summary>Where the line passes at <paramref name="height"/> above the ground.</summary>
    public MapPoint At(double height) =>
        new(Ground.X + (ShiftPerHeight.X * height), Ground.Y + (ShiftPerHeight.Y * height));
}
