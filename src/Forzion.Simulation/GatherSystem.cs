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

    /// <summary>Carrying its load to the nearest drop-off point of its Player.</summary>
    ToDropOff = 3,
}

/// <summary>
/// Runs the gather cycle of every Villager that has a resource source: walk up to the source,
/// take from it until the load is full, carry the load to the nearest drop-off point of the
/// Player, hand it over and walk back to the source.
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
                    TakeFromSource(state, state.FindResourceSource(unit.GatherSource!.Value)!, unit);
                    break;
                case GatherPhase.ToDropOff when !unit.IsMoving:
                    Deliver(state, unit);
                    break;
            }
        }
    }

    private static void TakeFromSource(MatchState state, ResourceSourceState source, UnitState villager)
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

        if (villager.CarriedAmount == Balance.VillagerCarryCapacity)
        {
            CarryToDropOff(state, villager);
        }
    }

    /// <summary>Sends the Villager walking to the drop-off point of its Player nearest to it.</summary>
    private static void CarryToDropOff(MatchState state, UnitState villager)
    {
        var from = villager.Position.Cell;
        var nearest = state.Buildings
            .Where(building => building.Owner == villager.Owner && building.IsDropOff)
            .Select(building => building.NearestCellTo(from))
            .MinBy(cell => SquaredDistance(cell, from));

        villager.GatherPhase = GatherPhase.ToDropOff;
        MovementSystem.WalkTo(state.Map, villager, nearest);
    }

    private static void Deliver(MatchState state, UnitState villager)
    {
        state.FindPlayer(villager.Owner)!.Receive(villager.CarriedResource, villager.CarriedAmount);
        villager.CarriedAmount = 0;
        GatherFrom(state.Map, villager, state.FindResourceSource(villager.GatherSource!.Value)!);
    }

    private static int SquaredDistance(CellPosition a, CellPosition b) =>
        ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));
}
