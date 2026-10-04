using Forzion.Simulation.Tests.Movement;

namespace Forzion.Simulation.Tests.Economy;

/// <summary>Helpers of the economy tests, built on the simulation's public interface only.</summary>
internal static class Gather
{
    /// <summary>More ticks than any test of gathering takes.</summary>
    public const int TickLimit = 20_000;

    /// <summary>The source of the given Resource nearest to the Cell; between sources equally near, the one with the lowest ID.</summary>
    public static ResourceSourceState NearestSource(MatchState state, CellPosition cell, ResourceKind kind) =>
        state.ResourceSources
            .Where(source => source.Kind == kind)
            .OrderBy(source => Walk.SquaredDistance(source.Cell, cell))
            .ThenBy(source => source.Id.Value)
            .First();

    /// <summary>The source with the given ID, or null once it has been depleted.</summary>
    public static ResourceSourceState? FindSource(MatchState state, EntityId id) =>
        state.ResourceSources.SingleOrDefault(source => source.Id == id);

    /// <summary>Ticks the match until the condition holds, failing the test after <see cref="TickLimit"/> ticks.</summary>
    public static void Until(Match match, Func<bool> condition)
    {
        for (var tick = 0; tick < TickLimit; tick++)
        {
            match.Tick();

            if (condition())
            {
                return;
            }
        }

        throw new InvalidOperationException($"The condition still did not hold after {TickLimit} ticks.");
    }
}
