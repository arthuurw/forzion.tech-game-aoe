using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Where things of the map go in the 3D world. One Cell is one world unit: the map's X runs
/// along the world's X axis, the map's Y along the world's Z axis, and the ground is at Y = 0.
/// </summary>
/// <remarks>Every <c>height</c> is in world units above the ground.</remarks>
public static class WorldSpace
{
    /// <summary>The world point above a point of the map.</summary>
    public static Vector3 ToWorld(MapPoint point, float height = 0) => new((float)point.X, height, (float)point.Y);

    /// <summary>The world point above the centre of a Cell.</summary>
    public static Vector3 CentreOf(CellPosition cell, float height = 0) => new(cell.X + 0.5f, height, cell.Y + 0.5f);

    /// <summary>The world point above the centre of a building's footprint.</summary>
    public static Vector3 CentreOf(BuildingState building, float height = 0) => ToWorld(MapPoint.CentreOf(building), height);
}
