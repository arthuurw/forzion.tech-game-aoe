using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// A point on the map in Cells, like the simulation's <c>MapPosition</c> but in floating point:
/// what the presentation draws, never fed back into the simulation.
/// </summary>
public readonly record struct MapPoint(double X, double Y)
{
    /// <summary>The centre of a building's footprint.</summary>
    public static MapPoint CentreOf(BuildingState building)
    {
        ArgumentNullException.ThrowIfNull(building);

        return new(building.Origin.X + (building.Width / 2.0), building.Origin.Y + (building.Height / 2.0));
    }
}
