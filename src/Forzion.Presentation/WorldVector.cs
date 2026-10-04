namespace Forzion.Presentation;

/// <summary>
/// A point or direction in the 3D world the game draws, in world units: Y is height, and X and
/// Z run across the map as the map's X and Y. One world unit is one Cell.
/// </summary>
public readonly record struct WorldVector(double X, double Y, double Z);
