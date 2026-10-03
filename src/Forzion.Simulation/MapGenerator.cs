namespace Forzion.Simulation;

/// <summary>What generation decided beyond the Cells it filled in the map.</summary>
/// <param name="Homes">The Cells the Players' Town Centers are centred on, the first Player's first.</param>
/// <param name="Sources">The resource sources, by row and then by column.</param>
internal sealed record GeneratedMap(
    IReadOnlyList<CellPosition> Homes,
    IReadOnlyList<(CellPosition Cell, ResourceKind Kind)> Sources);

/// <summary>
/// Generates the map of a match from the match's random generator, so the same seed always
/// gives the same map.
/// </summary>
/// <remarks>
/// <para>
/// Symmetry holds by construction: everything is placed in pairs, on a Cell and on its mirror
/// (<see cref="MapState.Mirror"/>), and only when both are free.
/// </para>
/// <para>
/// Reach holds by construction too. Around each home lies a clearing that only the home's own
/// resource sources may occupy, and those never touch one another, not even by a corner, nor
/// the Town Center, nor the edge of the clearing. Lone blocked Cells cannot fence anything in,
/// so every one of them can be walked up to from anywhere in the clearing.
/// </para>
/// </remarks>
internal static class MapGenerator
{
    /// <summary>The map has one symmetry, a half-turn, which can only pair two homes.</summary>
    public const int MaximumPlayers = 2;

    /// <summary>Smallest width and height, in Cells, that keep the two clearings apart.</summary>
    public const int MinimumSize = 32;

    // Distance, in Cells, from the map's first corner to the nearest Cell a home may be
    // centred on, and the number of Cells past it the seed may push the home on each axis.
    private const int HomeMargin = 7;
    private const int HomeJitter = 3;

    // Distances are counted in king's moves from the home Cell. The clearing reaches out to
    // ClearingRadius; home sources lie in the ring between the two source distances, clear of
    // the Town Center and of the Villagers beside it, and inside the clearing's outer ring.
    private const int ClearingRadius = 6;
    private const int NearestHomeSource = 3;
    private const int FarthestHomeSource = 5;

    private static readonly ResourceKind[] ResourceKinds = [ResourceKind.Food, ResourceKind.Wood, ResourceKind.Gold];

    /// <summary>Fills <paramref name="map"/>, which must be all free, and returns what else was decided.</summary>
    public static GeneratedMap Generate(MapState map, MatchRandom random)
    {
        var home = new CellPosition(
            HomeMargin + random.NextInt(HomeJitter),
            HomeMargin + random.NextInt(HomeJitter));
        var sources = new List<(CellPosition Cell, ResourceKind Kind)>();

        PlaceHomeSources(map, random, home, sources);

        return new GeneratedMap(
            [home, map.Mirror(home)],
            sources.OrderBy(source => source.Cell.Y).ThenBy(source => source.Cell.X).ToList());
    }

    private static void PlaceHomeSources(
        MapState map, MatchRandom random, CellPosition home, List<(CellPosition Cell, ResourceKind Kind)> sources)
    {
        var candidates = new List<CellPosition>();

        for (var y = home.Y - FarthestHomeSource; y <= home.Y + FarthestHomeSource; y++)
        {
            for (var x = home.X - FarthestHomeSource; x <= home.X + FarthestHomeSource; x++)
            {
                var cell = new CellPosition(x, y);

                if (Distance(cell, home) >= NearestHomeSource)
                {
                    candidates.Add(cell);
                }
            }
        }

        foreach (var kind in ResourceKinds)
        {
            for (var placed = 0; placed < Balance.HomeSourcesPerResource; placed++)
            {
                var cell = candidates[random.NextInt(candidates.Count)];

                // Dropping the neighbours too keeps the sources from touching one another.
                candidates.RemoveAll(candidate => Distance(candidate, cell) <= 1);
                PlaceSourcePair(map, cell, kind, sources);
            }
        }
    }

    private static void PlaceSourcePair(
        MapState map, CellPosition cell, ResourceKind kind, List<(CellPosition Cell, ResourceKind Kind)> sources)
    {
        var mirror = map.Mirror(cell);

        map[cell] = CellKind.ResourceSource;
        map[mirror] = CellKind.ResourceSource;
        sources.Add((cell, kind));
        sources.Add((mirror, kind));
    }

    /// <summary>Distance in king's moves.</summary>
    private static int Distance(CellPosition a, CellPosition b) =>
        Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
