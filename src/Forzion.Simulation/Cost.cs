namespace Forzion.Simulation;

/// <summary>An amount of each Resource, paid as a whole: what a building costs to place.</summary>
/// <param name="Food">Food to pay.</param>
/// <param name="Wood">Wood to pay.</param>
/// <param name="Gold">Gold to pay.</param>
public readonly record struct Cost(int Food, int Wood, int Gold)
{
    /// <summary>How much of the given Resource the cost takes.</summary>
    public int AmountOf(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => Food,
        ResourceKind.Wood => Wood,
        ResourceKind.Gold => Gold,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
