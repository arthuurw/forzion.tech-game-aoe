namespace Forzion.Simulation.Tests.Movement;

/// <summary>Helpers of the movement tests, built on the simulation's public interface only.</summary>
internal static class Walk
{
    /// <summary>More ticks than any walk across a test map takes.</summary>
    public const int TickLimit = 5000;

    /// <summary>The middle one of the first Player's starting Villagers, which stands beside the middle of the Town Center.</summary>
    public static UnitState MiddleVillager(Match match) => match.State.Units[1];

    /// <summary>
    /// The Cell on the far side of the first Player's Town Center from the middle Villager:
    /// the straight line between the two crosses the building.
    /// </summary>
    public static CellPosition BehindTownCenter(Match match)
    {
        var townCenter = match.State.Buildings[0];
        var villager = MiddleVillager(match).Position.Cell;
        var centre = new CellPosition(
            townCenter.Origin.X + (townCenter.Width / 2),
            townCenter.Origin.Y + (townCenter.Height / 2));

        return new CellPosition((2 * centre.X) - villager.X, (2 * centre.Y) - villager.Y);
    }

    /// <summary>
    /// Ticks the match until the unit stops moving and returns the Cells it stood on after
    /// each tick, in order and without consecutive repeats, starting with the Cell it set out from.
    /// </summary>
    public static List<CellPosition> UntilStopped(Match match, UnitState unit)
    {
        var visited = new List<CellPosition> { unit.Position.Cell };

        // The first tick applies the pending command; from then on the unit says whether it is still walking.
        for (var tick = 0; tick < TickLimit; tick++)
        {
            match.Tick();

            if (unit.Position.Cell != visited[^1])
            {
                visited.Add(unit.Position.Cell);
            }

            if (!unit.IsMoving)
            {
                return visited;
            }
        }

        throw new InvalidOperationException($"The unit was still walking after {TickLimit} ticks.");
    }
}
