namespace Forzion.Simulation;

/// <summary>What generation decided beyond the Cells it filled in the map.</summary>
/// <param name="Homes">The Cells the Players' Town Centers are centred on, the first Player's first.</param>
/// <param name="Sources">The resource sources, by row and then by column.</param>
internal sealed record GeneratedMap(
    IReadOnlyList<CellPosition> Homes,
    IReadOnlyList<SourcePlacement> Sources);

/// <summary>Where generation put a resource source and which Resource it holds.</summary>
internal readonly record struct SourcePlacement(CellPosition Cell, ResourceKind Kind);

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
/// <para>
/// Outside the clearings the seed scatters more sources and the obstacles. A scattering that
/// cuts one home off from the other is thrown away and drawn again; after
/// <see cref="ScatterAttempts"/> failures the map is left open, which always connects them.
/// </para>
/// <para>
/// A scattered source can still end up walled in by forest or water. Such sources are
/// dropped once the scattering is kept, so every source left has a free Cell beside it that
/// both homes reach.
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

    // What is scattered outside the clearings, in proportion to the map's area.
    private const int CellsPerFarSource = 512;
    private const int CellsPerObstacle = 128;

    // An obstacle is a random walk that turns every Cell it steps on into forest or water.
    private const int ShortestObstacleWalk = 16;
    private const int ObstacleWalkSpread = 33;

    private const int ScatterAttempts = 8;

    private static readonly ResourceKind[] ResourceKinds = [ResourceKind.Food, ResourceKind.Wood, ResourceKind.Gold];

    /// <summary>Fills <paramref name="map"/>, which must be all free, and returns what else was decided.</summary>
    public static GeneratedMap Generate(MapState map, MatchRandom random)
    {
        var home = new CellPosition(
            HomeMargin + random.NextInt(HomeJitter),
            HomeMargin + random.NextInt(HomeJitter));
        var sources = new List<SourcePlacement>();

        PlaceHomeSources(map, random, home, sources);

        for (var attempt = 0; attempt < ScatterAttempts; attempt++)
        {
            var homeSources = sources.Count;
            var scattered = new List<CellPosition>();

            ScatterFarSources(map, random, home, sources, scattered);
            ScatterObstacles(map, random, home, scattered);

            if (ReachableFrom(map, home).Contains(map.Mirror(home)))
            {
                break;
            }

            foreach (var cell in scattered)
            {
                map[cell] = CellKind.Free;
            }

            sources.RemoveRange(homeSources, sources.Count - homeSources);
        }

        DropWalledInSources(map, home, sources);

        return new GeneratedMap(
            [home, map.Mirror(home)],
            sources.OrderBy(source => source.Cell.Y).ThenBy(source => source.Cell.X).ToList());
    }

    private static void PlaceHomeSources(
        MapState map, MatchRandom random, CellPosition home, List<SourcePlacement> sources)
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

    private static void ScatterFarSources(
        MapState map,
        MatchRandom random,
        CellPosition home,
        List<SourcePlacement> sources,
        List<CellPosition> scattered)
    {
        var count = map.Width * map.Height / CellsPerFarSource;

        for (var i = 0; i < count; i++)
        {
            var kind = ResourceKinds[random.NextInt(ResourceKinds.Length)];
            var cell = new CellPosition(random.NextInt(map.Width), random.NextInt(map.Height));

            if (CanScatterOn(map, home, cell))
            {
                PlaceSourcePair(map, cell, kind, sources);
                scattered.Add(cell);
                scattered.Add(map.Mirror(cell));
            }
        }
    }

    private static void ScatterObstacles(MapState map, MatchRandom random, CellPosition home, List<CellPosition> scattered)
    {
        var count = map.Width * map.Height / CellsPerObstacle;

        for (var i = 0; i < count; i++)
        {
            var kind = random.NextInt(2) == 0 ? CellKind.Forest : CellKind.Water;
            var x = random.NextInt(map.Width);
            var y = random.NextInt(map.Height);
            var steps = ShortestObstacleWalk + random.NextInt(ObstacleWalkSpread);

            for (var step = 0; step < steps; step++)
            {
                var cell = new CellPosition(x, y);

                if (CanScatterOn(map, home, cell))
                {
                    var mirror = map.Mirror(cell);

                    map[cell] = kind;
                    map[mirror] = kind;
                    scattered.Add(cell);
                    scattered.Add(mirror);
                }

                // A step that would leave the map stays where it is.
                switch (random.NextInt(4))
                {
                    case 0:
                        x = Math.Min(x + 1, map.Width - 1);
                        break;
                    case 1:
                        x = Math.Max(x - 1, 0);
                        break;
                    case 2:
                        y = Math.Min(y + 1, map.Height - 1);
                        break;
                    default:
                        y = Math.Max(y - 1, 0);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Whether a scattered pair may go on the Cell and its mirror: both free, outside both
    /// clearings and not one and the same Cell. The map is symmetric at every step, so asking
    /// about the Cell answers for its mirror too.
    /// </summary>
    private static bool CanScatterOn(MapState map, CellPosition home, CellPosition cell) =>
        map[cell] == CellKind.Free
        && cell != map.Mirror(cell)
        && Distance(cell, home) > ClearingRadius
        && Distance(cell, map.Mirror(home)) > ClearingRadius;

    /// <summary>
    /// The free Cells linked to the home by free Cells that share a side. The Town Centers are
    /// not on the map yet, so the home Cells themselves are free; each sits in its open
    /// clearing, so reaching the home Cell is reaching the clearing.
    /// </summary>
    private static HashSet<CellPosition> ReachableFrom(MapState map, CellPosition home)
    {
        var reached = new HashSet<CellPosition> { home };
        var frontier = new Queue<CellPosition>();

        frontier.Enqueue(home);

        while (frontier.Count > 0)
        {
            foreach (var next in SideNeighbours(frontier.Dequeue()))
            {
                if (map.Contains(next) && map[next] == CellKind.Free && reached.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        return reached;
    }

    /// <summary>
    /// Frees the Cells of the sources no Cell reachable from the homes touches and forgets
    /// those sources. The homes are linked, so what one reaches the other reaches too, and on
    /// a symmetric map that is symmetric as well: a source and its mirror go together.
    /// </summary>
    private static void DropWalledInSources(
        MapState map, CellPosition home, List<SourcePlacement> sources)
    {
        var reached = ReachableFrom(map, home);
        var walledIn = sources.Where(source => !SideNeighbours(source.Cell).Any(reached.Contains)).ToHashSet();

        foreach (var source in walledIn)
        {
            map[source.Cell] = CellKind.Free;
        }

        sources.RemoveAll(walledIn.Contains);
    }

    private static CellPosition[] SideNeighbours(CellPosition cell) =>
    [
        new(cell.X + 1, cell.Y), new(cell.X - 1, cell.Y), new(cell.X, cell.Y + 1), new(cell.X, cell.Y - 1),
    ];

    private static void PlaceSourcePair(
        MapState map, CellPosition cell, ResourceKind kind, List<SourcePlacement> sources)
    {
        var mirror = map.Mirror(cell);

        map[cell] = CellKind.ResourceSource;
        map[mirror] = CellKind.ResourceSource;
        sources.Add(new SourcePlacement(cell, kind));
        sources.Add(new SourcePlacement(mirror, kind));
    }

    /// <summary>Distance in king's moves.</summary>
    private static int Distance(CellPosition a, CellPosition b) =>
        Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
