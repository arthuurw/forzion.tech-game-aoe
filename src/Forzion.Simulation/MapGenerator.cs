namespace Forzion.Simulation;

/// <summary>What generation decided beyond the forest and water it laid on the map.</summary>
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
    // These and the clearing's radius stay here, not in Balance: MinimumSize is worked out
    // from them, so they are the generator's geometry rather than tuning.
    private const int HomeMargin = 7;
    private const int HomeJitter = 3;

    // Distance, in king's moves from the home Cell, the clearing reaches out to. It must
    // exceed Balance.FarthestHomeSource, so home sources never touch the clearing's edge.
    private const int ClearingRadius = 6;

    private const int ScatterAttempts = 8;

    private static readonly ResourceKind[] ResourceKinds = [ResourceKind.Food, ResourceKind.Wood, ResourceKind.Gold];

    /// <summary>
    /// Lays forest and water on <paramref name="map"/>, which must be all free, and returns
    /// where the homes and the resource sources go.
    /// </summary>
    /// <remarks>
    /// Generation works on a plan of its own, where source Cells are marked too so that
    /// scattering and reach take them into account. Only the forest and water are copied to
    /// <paramref name="map"/>: a source's Cell is marked by whoever creates the source, in the
    /// same step (EST-8).
    /// </remarks>
    public static GeneratedMap Generate(MapState map, MatchRandom random)
    {
        var plan = new MapState(map.Width, map.Height);
        var home = new CellPosition(
            HomeMargin + random.NextInt(HomeJitter),
            HomeMargin + random.NextInt(HomeJitter));
        var sources = new List<SourcePlacement>();

        PlaceHomeSources(plan, random, home, sources);

        for (var attempt = 0; attempt < ScatterAttempts; attempt++)
        {
            var homeSources = sources.Count;
            var scattered = new List<CellPosition>();

            ScatterFarSources(plan, random, home, sources, scattered);
            ScatterObstacles(plan, random, home, scattered);

            if (ReachableFrom(plan, home).Contains(plan.Mirror(home)))
            {
                break;
            }

            foreach (var cell in scattered)
            {
                plan[cell] = CellKind.Free;
            }

            sources.RemoveRange(homeSources, sources.Count - homeSources);
        }

        DropWalledInSources(plan, home, sources);
        CopyObstacles(plan, map);

        return new GeneratedMap(
            [home, plan.Mirror(home)],
            sources.OrderBy(source => source.Cell.Y).ThenBy(source => source.Cell.X).ToList());
    }

    private static void CopyObstacles(MapState plan, MapState map)
    {
        for (var y = 0; y < plan.Height; y++)
        {
            for (var x = 0; x < plan.Width; x++)
            {
                var cell = new CellPosition(x, y);

                if (plan[cell] is CellKind.Forest or CellKind.Water)
                {
                    map[cell] = plan[cell];
                }
            }
        }
    }

    private static void PlaceHomeSources(
        MapState map, MatchRandom random, CellPosition home, List<SourcePlacement> sources)
    {
        var candidates = new List<CellPosition>();

        for (var y = home.Y - Balance.FarthestHomeSource; y <= home.Y + Balance.FarthestHomeSource; y++)
        {
            for (var x = home.X - Balance.FarthestHomeSource; x <= home.X + Balance.FarthestHomeSource; x++)
            {
                var cell = new CellPosition(x, y);

                if (Distance(cell, home) >= Balance.NearestHomeSource)
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
        var count = map.Width * map.Height / Balance.CellsPerFarSource;

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
        var count = map.Width * map.Height / Balance.CellsPerObstacle;

        for (var i = 0; i < count; i++)
        {
            var kind = random.NextInt(2) == 0 ? CellKind.Forest : CellKind.Water;
            var x = random.NextInt(map.Width);
            var y = random.NextInt(map.Height);
            var steps = Balance.ShortestObstacleWalk + random.NextInt(Balance.ObstacleWalkSpread);

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

                // A step off the map is clamped rather than drawn again, so every step costs
                // exactly one draw and the walk's length alone sets how far the random
                // generator advances.
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
        map.IsFree(cell)
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
            var cell = frontier.Dequeue();

            foreach (var step in CellStep.Sides)
            {
                var next = step.From(cell);

                if (map.IsFree(next) && reached.Add(next))
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
        var walledIn = sources
            .Where(source => !CellStep.Sides.Any(step => reached.Contains(step.From(source.Cell))))
            .ToHashSet();

        foreach (var source in walledIn)
        {
            map[source.Cell] = CellKind.Free;
        }

        sources.RemoveAll(walledIn.Contains);
    }

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
