namespace Forzion.Simulation;

/// <summary>What a Villager is doing towards gathering Resources.</summary>
public enum GatherPhase
{
    /// <summary>Not gathering.</summary>
    None = 0,

    /// <summary>Walking up to its resource source.</summary>
    ToSource = 1,

    /// <summary>Standing beside its resource source and taking from it.</summary>
    Gathering = 2,
}

/// <summary>
/// Runs the gather cycle of every Villager that has a resource source: walk up to the source,
/// then take from it.
/// </summary>
internal sealed class GatherSystem : ISystem
{
    /// <summary>Sends the Villager walking up to the source to gather from it.</summary>
    public static void GatherFrom(MapState map, UnitState villager, ResourceSourceState source)
    {
        villager.GatherSource = source.Id;
        villager.GatherPhase = GatherPhase.ToSource;
        villager.GatherProgress = 0;
        MovementSystem.WalkTo(map, villager, source.Cell);
    }

    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var unit in state.Units)
        {
            switch (unit.GatherPhase)
            {
                case GatherPhase.ToSource when !unit.IsMoving:
                    unit.GatherPhase = GatherPhase.Gathering;
                    break;
                case GatherPhase.Gathering:
                    TakeFromSource(state.FindResourceSource(unit.GatherSource!.Value)!, unit);
                    break;
            }
        }
    }

    private static void TakeFromSource(ResourceSourceState source, UnitState villager)
    {
        villager.GatherProgress++;

        if (villager.GatherProgress < Balance.GatherTicksPerUnit(source.Kind))
        {
            return;
        }

        villager.GatherProgress = 0;
        source.Amount--;
        villager.CarriedResource = source.Kind;
        villager.CarriedAmount++;
    }
}
