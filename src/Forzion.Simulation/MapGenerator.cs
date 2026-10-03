namespace Forzion.Simulation;

/// <summary>
/// Generates the map of a match from the match's random generator, so the same seed always
/// gives the same map.
/// </summary>
/// <remarks>
/// Symmetry holds by construction: everything is placed in pairs, on a Cell and on its mirror
/// (<see cref="MapState.Mirror"/>), and only when both are free.
/// </remarks>
internal static class MapGenerator
{
    /// <summary>The map has one symmetry, a half-turn, which can only pair two homes.</summary>
    public const int MaximumPlayers = 2;

    /// <summary>Smallest width and height, in Cells, that keep the two homes apart.</summary>
    public const int MinimumSize = 32;

    // Distance, in Cells, from the map's first corner to the nearest Cell a home may be
    // centred on, and the number of Cells past it the seed may push the home on each axis.
    private const int HomeMargin = 7;
    private const int HomeJitter = 3;

    /// <summary>
    /// Fills <paramref name="map"/> and returns the homes: the Cells the Players' Town Centers
    /// are centred on, the first Player's first. The second is the mirror of the first.
    /// </summary>
    public static IReadOnlyList<CellPosition> Generate(MapState map, MatchRandom random)
    {
        var home = new CellPosition(
            HomeMargin + random.NextInt(HomeJitter),
            HomeMargin + random.NextInt(HomeJitter));

        return [home, map.Mirror(home)];
    }
}
