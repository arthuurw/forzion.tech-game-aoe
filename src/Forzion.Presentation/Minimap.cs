using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// The minimap: the whole map drawn small in a box of the screen, as large as fits and centred
/// across the room to spare. It places points of the map in the box and back, the second for
/// a click that moves the camera. Points in the box are in pixels from its top left corner.
/// </summary>
public sealed class Minimap
{
    private readonly int mapWidth;
    private readonly int mapHeight;

    /// <param name="map">The map drawn.</param>
    /// <param name="width">Width of the box in pixels.</param>
    /// <param name="height">Height of the box in pixels.</param>
    public Minimap(MapState map, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        mapWidth = map.Width;
        mapHeight = map.Height;
        Scale = Math.Min(width / map.Width, height / map.Height);
        Corner = new ScreenPoint((width - (map.Width * Scale)) / 2, (height - (map.Height * Scale)) / 2);
    }

    /// <summary>Pixels of the box per Cell, the same across and down.</summary>
    public double Scale { get; }

    /// <summary>Where the corner of the map at Cell (0, 0) falls in the box.</summary>
    public ScreenPoint Corner { get; }

    /// <summary>Where a point of the map falls in the box.</summary>
    public ScreenPoint ToMinimap(MapPoint point) => new(Corner.X + (point.X * Scale), Corner.Y + (point.Y * Scale));

    /// <summary>
    /// The point of the map drawn at a point of the box. A point in the room beside the map
    /// gives the nearest point on its edge, so a click anywhere in the box means a place.
    /// </summary>
    public MapPoint ToMap(ScreenPoint point) => new(
        Math.Clamp((point.X - Corner.X) / Scale, 0, mapWidth),
        Math.Clamp((point.Y - Corner.Y) / Scale, 0, mapHeight));
}
