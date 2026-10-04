using Forzion.Simulation.Tests.Matches;

namespace Forzion.Simulation.Tests.Maps;

/// <summary>Questions the map tests ask of a match's public state.</summary>
internal static class MapProbe
{
    public static IEnumerable<CellPosition> AllCells(MapState map)
    {
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                yield return new CellPosition(x, y);
            }
        }
    }

    /// <summary>The Cell a half-turn around the centre of the map takes <paramref name="cell"/> to.</summary>
    public static CellPosition Mirror(MapState map, CellPosition cell) =>
        new(map.Width - 1 - cell.X, map.Height - 1 - cell.Y);

    /// <summary>Whether the two Cells touch, by a side or by a corner.</summary>
    public static bool Touch(CellPosition a, CellPosition b) =>
        a != b && Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Y - b.Y) <= 1;

    /// <summary>Whether the Cell touches one of the footprint's Cells, by a side or by a corner, without being one of them.</summary>
    public static bool IsBeside(IReadOnlySet<CellPosition> footprint, CellPosition cell) =>
        !footprint.Contains(cell) && footprint.Any(other => Touch(cell, other));

    /// <summary>Whether the Cell touches the building's footprint, by a side or by a corner, without being under it.</summary>
    public static bool IsBeside(BuildingState building, CellPosition cell) => IsBeside(Footprint(building).ToHashSet(), cell);

    /// <summary>The Cells of the square of the given side whose lowest corner is <paramref name="origin"/>.</summary>
    public static IEnumerable<CellPosition> Square(CellPosition origin, int side)
    {
        for (var y = origin.Y; y < origin.Y + side; y++)
        {
            for (var x = origin.X; x < origin.X + side; x++)
            {
                yield return new CellPosition(x, y);
            }
        }
    }

    /// <summary>Whether the two Cells share a side.</summary>
    public static bool AreNeighbours(CellPosition a, CellPosition b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;

    public static IEnumerable<CellPosition> NeighboursOf(MapState map, CellPosition cell)
    {
        CellPosition[] candidates =
        [
            new(cell.X + 1, cell.Y), new(cell.X - 1, cell.Y), new(cell.X, cell.Y + 1), new(cell.X, cell.Y - 1),
        ];

        return candidates.Where(map.Contains);
    }

    /// <summary>
    /// The Cells a unit starting on <paramref name="start"/> can walk to, moving between free
    /// Cells that share a side, with the <paramref name="blocked"/> Cells taken as well: those
    /// of a building not yet placed.
    /// </summary>
    public static HashSet<CellPosition> ReachableFrom(
        MapState map, CellPosition start, IReadOnlySet<CellPosition>? blocked = null)
    {
        var reached = new HashSet<CellPosition> { start };
        var frontier = new Queue<CellPosition>([start]);

        while (frontier.Count > 0)
        {
            foreach (var next in NeighboursOf(map, frontier.Dequeue()))
            {
                if (map[next] == CellKind.Free && blocked?.Contains(next) != true && reached.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        return reached;
    }

    /// <summary>The Resources every Villager of the Player can walk up to a source of.</summary>
    public static IEnumerable<ResourceKind> ReachableResources(MatchState state, PlayerId player)
    {
        var villagers = state.UnitsOf(player).ToList();
        var reachable = state.ResourceSources.Select(source => source.Kind).ToHashSet();

        foreach (var villager in villagers)
        {
            var reached = ReachableFrom(state.Map, villager.Position.Cell);

            reachable.IntersectWith(state.ResourceSources
                .Where(source => NeighboursOf(state.Map, source.Cell).Any(reached.Contains))
                .Select(source => source.Kind));
        }

        return villagers.Count > 0 ? reachable : [];
    }

    public static IEnumerable<CellPosition> Footprint(BuildingState building)
    {
        for (var y = building.Origin.Y; y < building.Origin.Y + building.Height; y++)
        {
            for (var x = building.Origin.X; x < building.Origin.X + building.Width; x++)
            {
                yield return new CellPosition(x, y);
            }
        }
    }
}
