namespace Forzion.Simulation;

/// <summary>
/// What a Villager carries: an amount of a single Resource, handed to its Player only when
/// delivered at a drop-off point.
/// </summary>
/// <param name="Resource">The Resource carried. Meaningless while <paramref name="Amount"/> is zero.</param>
/// <param name="Amount">How much of it is carried.</param>
public readonly record struct Load(ResourceKind Resource, int Amount)
{
    /// <summary>Nothing carried.</summary>
    public static Load Empty => default;
}
