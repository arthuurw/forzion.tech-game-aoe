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

    /// <summary>
    /// What the minimap draws over the terrain, in the order to draw it: the resource sources
    /// over their Cells, the buildings over their footprints, then the units, each a Cell
    /// across and centred where it is drawn, so that a unit beside a building shows over it.
    /// </summary>
    /// <param name="state">The match.</param>
    /// <param name="positionOf">Where a unit is drawn in the current frame.</param>
    public IReadOnlyList<MinimapMark> MarksOf(MatchState state, Func<UnitState, MapPoint> positionOf)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(positionOf);

        var sources = state.ResourceSources.Select(source => new ResourceSourceMark(
            ToMinimap(new MapPoint(source.Cell.X, source.Cell.Y)),
            Scale,
            Scale,
            source.Kind));
        var buildings = state.Buildings.Select(building => new PlayerMark(
            ToMinimap(new MapPoint(building.Origin.X, building.Origin.Y)),
            building.Width * Scale,
            building.Height * Scale,
            building.Owner));
        var units = state.Units.Select(unit => new PlayerMark(
            CornerOfCellAround(positionOf(unit)),
            Scale,
            Scale,
            unit.Owner));

        return [.. sources, .. buildings, .. units];
    }

    /// <summary>The corner of a Cell-sized square centred on the point.</summary>
    private ScreenPoint CornerOfCellAround(MapPoint centre) => ToMinimap(new MapPoint(centre.X - 0.5, centre.Y - 0.5));
}

/// <summary>A rectangle the minimap draws over the terrain, in pixels of its box.</summary>
/// <param name="Corner">The rectangle's top left corner.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public abstract record MinimapMark(ScreenPoint Corner, double Width, double Height);

/// <summary>A unit or building on the minimap, drawn in the colour of its owner.</summary>
/// <param name="Owner">The Player who owns the unit or building.</param>
public sealed record PlayerMark(ScreenPoint Corner, double Width, double Height, PlayerId Owner)
    : MinimapMark(Corner, Width, Height);

/// <summary>A resource source on the minimap, drawn in the colour of its Resource.</summary>
/// <param name="Resource">The Resource the source holds.</param>
public sealed record ResourceSourceMark(ScreenPoint Corner, double Width, double Height, ResourceKind Resource)
    : MinimapMark(Corner, Width, Height);
