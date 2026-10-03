namespace Forzion.Simulation;

/// <summary>A Cell of the map by column and row, counted from 0 at the map's first corner.</summary>
public readonly record struct CellPosition(int X, int Y);
