namespace Forzion.Presentation;

/// <summary>
/// A point on the map in Cells, like the simulation's <c>MapPosition</c> but in floating point:
/// what the presentation draws, never fed back into the simulation.
/// </summary>
public readonly record struct MapPoint(double X, double Y);
