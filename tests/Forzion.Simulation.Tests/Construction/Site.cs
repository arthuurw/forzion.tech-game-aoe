using Forzion.Simulation.Tests.Economy;
using Forzion.Simulation.Tests.Maps;
using Forzion.Simulation.Tests.Matches;
using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Construction;

/// <summary>Helpers of the construction tests, built on the simulation's public interface only.</summary>
internal static class Site
{
    /// <summary>The Player's Villagers, in ID order.</summary>
    public static List<UnitState> VillagersOf(Match match, PlayerId player) =>
        match.State.UnitsOf(player).Where(unit => unit.Kind == UnitKind.Villager).ToList();

    /// <summary>
    /// Has every Villager of the Player gather Wood until the Player holds at least
    /// <paramref name="wood"/>, then stops them where they stand. They keep what they still carry.
    /// </summary>
    public static void Stockpile(Match match, PlayerId player, int wood)
    {
        var state = match.State;
        var villagers = VillagersOf(match, player);
        var source = Gather.NearestSource(state, villagers[1].Position.Cell, ResourceKind.Wood);
        match.Enqueue(new GatherCommand(player, villagers.Select(villager => villager.Id).ToList(), source.Id));

        Gather.Until(match, () => state.Players[player.Value - 1].AmountOf(ResourceKind.Wood) >= wood);
        Halt(match, villagers);
    }

    /// <summary>
    /// Gathers the Wood for a building of the given kind with the Player's Villagers, places it
    /// near the Player's Town Center with the given builders and ticks once to apply the
    /// placement. Returns the new construction site.
    /// </summary>
    public static BuildingState Place(Match match, PlayerId player, BuildingKind kind, IReadOnlyList<EntityId> builders)
    {
        var state = match.State;
        var townCenter = state.Buildings.First(building => building.Owner == player && building.Kind == BuildingKind.TownCenter);
        Stockpile(match, player, Match.BuildingCost(kind).Wood);
        var origin = FreeOriginNear(state, townCenter.Origin, Match.BuildingSize(kind));
        match.Enqueue(new PlaceBuildingCommand(player, kind, origin, builders));
        match.Tick();

        return state.Buildings[^1];
    }

    /// <summary>Orders each of the units to the Cell it is in and ticks until all of them stand still.</summary>
    public static void Halt(Match match, IEnumerable<UnitState> units)
    {
        var halted = units.ToList();

        foreach (var unit in halted)
        {
            match.Enqueue(new MoveCommand(unit.Owner, [unit.Id], unit.Position.Cell));
        }

        Gather.Until(match, () => halted.All(unit => !unit.IsMoving));
    }

    /// <summary>
    /// The origin nearest to <paramref name="near"/> of a square of free Cells of the given side,
    /// ringed by free Cells and with no unit standing on the square or its ring, so the
    /// building placed there can be walked around and reached from every side. Between origins
    /// equally near, the one with the lowest row, then the lowest column.
    /// </summary>
    public static CellPosition FreeOriginNear(MatchState state, CellPosition near, int side)
    {
        var map = state.Map;
        var candidates = new List<CellPosition>();

        for (var y = 1; y + side < map.Height; y++)
        {
            for (var x = 1; x + side < map.Width; x++)
            {
                candidates.Add(new CellPosition(x, y));
            }
        }

        return candidates
            .OrderBy(origin => Walk.SquaredDistance(origin, near))
            .ThenBy(origin => origin.Y)
            .ThenBy(origin => origin.X)
            .First(origin =>
            {
                var ringed = new CellPosition(origin.X - 1, origin.Y - 1);

                return Square(ringed, side + 2).All(cell => map[cell] == CellKind.Free)
                    && !state.Units.Any(unit => IsUnder(unit.Position.Cell, ringed, side + 2));
            });
    }

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

    /// <summary>
    /// The origin nearest to the unit of a House that can be placed now and whose side nearest
    /// to the unit is walled off: once the House stands, the unit can still walk to a Cell beside
    /// it, but every Cell it can walk to that is nearest in a straight line to the Cell of the
    /// footprint nearest to the unit lies away from the footprint.
    /// </summary>
    public static CellPosition OriginWalledOffOnItsNearSide(Match match, UnitState unit)
    {
        var map = match.State.Map;
        var size = Match.BuildingSize(BuildingKind.House);
        var start = unit.Position.Cell;

        return MapProbe.AllCells(map)
            .Where(origin => match.CanPlace(BuildingKind.House, origin))
            .OrderBy(origin => Walk.SquaredDistance(origin, start))
            .ThenBy(origin => origin.Y)
            .ThenBy(origin => origin.X)
            .First(origin =>
            {
                var footprint = Square(origin, size).ToHashSet();
                var nearSide = new CellPosition(
                    Math.Clamp(start.X, origin.X, origin.X + size - 1),
                    Math.Clamp(start.Y, origin.Y, origin.Y + size - 1));
                var reachable = ReachableAround(map, start, footprint);
                var nearest = reachable.Min(cell => Walk.SquaredDistance(cell, nearSide));

                return reachable.Any(cell => IsBeside(footprint, cell))
                    && !reachable.Any(cell => Walk.SquaredDistance(cell, nearSide) == nearest && IsBeside(footprint, cell));
            });
    }

    /// <summary>Whether the Cell touches one of the footprint's Cells, by a side or by a corner, without being one of them.</summary>
    private static bool IsBeside(HashSet<CellPosition> footprint, CellPosition cell) =>
        !footprint.Contains(cell) && footprint.Any(other => Gather.Touch(cell, other));

    /// <summary>
    /// The Cells a unit on <paramref name="start"/> can walk to, moving between free Cells that
    /// share a side, once the <paramref name="blocked"/> Cells are taken as well.
    /// </summary>
    private static HashSet<CellPosition> ReachableAround(MapState map, CellPosition start, HashSet<CellPosition> blocked)
    {
        var reached = new HashSet<CellPosition> { start };
        var frontier = new Queue<CellPosition>([start]);

        while (frontier.Count > 0)
        {
            foreach (var next in MapProbe.NeighboursOf(map, frontier.Dequeue()))
            {
                if (map[next] == CellKind.Free && !blocked.Contains(next) && reached.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        return reached;
    }

    private static bool IsUnder(CellPosition cell, CellPosition origin, int side) =>
        cell.X >= origin.X && cell.X < origin.X + side && cell.Y >= origin.Y && cell.Y < origin.Y + side;
}
