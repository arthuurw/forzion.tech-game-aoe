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
                    ReachSource(state, unit);
                    break;
                case GatherPhase.Gathering:
                    TakeFromSource(context, state.FindResourceSource(unit.GatherSource!.Value)!, unit);
                    break;
                case GatherPhase.ToDropOff when !unit.IsMoving:
                    Deliver(state, unit);
                    break;
            }
        }
    }

    /// <summary>
    /// The Villager has walked as far as it can towards its source: beside it, it starts
    /// gathering; short of it, the source cannot be reached and the Villager stands idle.
    /// </summary>
    private static void ReachSource(MatchState state, UnitState villager)
    {
        var source = state.FindResourceSource(villager.GatherSource!.Value)!;
        var cell = villager.Position.Cell;

        if (Math.Abs(cell.X - source.Cell.X) <= 1 && Math.Abs(cell.Y - source.Cell.Y) <= 1)
        {
            villager.GatherPhase = GatherPhase.Gathering;
        }
        else
        {
            StopGathering(state.Map, villager);
        }
    }

    private static void TakeFromSource(TickContext context, ResourceSourceState source, UnitState villager)
    {
        var state = context.State;

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

        if (source.Amount == 0)
        {
            Deplete(context, source);
        }
    }

    /// <summary>
    /// Takes the source off the map. Its Villagers move on to the nearest source of the same
    /// Resource within <see cref="Balance.SourceSearchRadius"/> of it, or stop and stand idle
    /// when there is none. Those already carrying a load away deliver it first.
    /// </summary>
    private static void Deplete(TickContext context, ResourceSourceState source)
    {
        var state = context.State;

        state.RemoveResourceSource(source);
        context.Emit(new ResourceSourceDepleted(source.Id));

        var replacement = NearestSourceAround(state, source.Kind, source.Cell);

        foreach (var unit in state.Units)
        {
            if (unit.GatherSource != source.Id)
            {
                continue;
            }

            unit.GatherSource = replacement?.Id;

            if (unit.GatherPhase == GatherPhase.ToDropOff)
            {
                continue;
            }

            if (replacement is null)
            {
                StopGathering(state.Map, unit);
            }
            else
            {
                GatherFrom(state.Map, unit, replacement);
            }
        }
    }

    /// <summary>
    /// The source of the Resource nearest to the Cell within <see cref="Balance.SourceSearchRadius"/>
    /// of it, or null when there is none. Between sources equally near, the one with the lowest ID.
    /// </summary>
    private static ResourceSourceState? NearestSourceAround(MatchState state, ResourceKind kind, CellPosition cell)
    {
        ResourceSourceState? nearest = null;
        var nearestDistance = (Balance.SourceSearchRadius * Balance.SourceSearchRadius) + 1;

        foreach (var source in state.ResourceSources)
        {
            var distance = SquaredDistance(source.Cell, cell);

            if (source.Kind == kind && distance < nearestDistance)
            {
                nearest = source;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <summary>
    /// The Villager stops gathering and stands idle on the Cell it is in, keeping whatever it
    /// carries.
    /// </summary>
    private static void StopGathering(MapState map, UnitState villager)
    {
        villager.StopGathering();
        MovementSystem.WalkTo(map, villager, villager.Position.Cell);
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

        if (villager.GatherSource is { } source)
        {
            GatherFrom(state.Map, villager, state.FindResourceSource(source)!);
        }
        else
        {
            StopGathering(state.Map, villager);
        }
    }

    private static int SquaredDistance(CellPosition a, CellPosition b) =>
        ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));
}
