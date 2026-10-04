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
    ToDropOffPoint = 3,
}

/// <summary>
/// Runs the gather cycle of every Villager that has a resource source: walk up to the source,
/// take from it until the load is full, carry the load to the nearest drop-off point of the
/// Player, hand it over and walk back to the source.
/// </summary>
internal sealed class GatherSystem : ISystem
{
    /// <summary>
    /// Sends the Villager walking up to the source to gather from it. A load of the source's
    /// Resource is kept, and when it is already full the Villager delivers it first. It stops
    /// building. A Villager already gathering from that very source goes on as it was, keeping
    /// its progress towards the next unit.
    /// </summary>
    public static void GatherFrom(MatchState state, UnitState villager, ResourceSourceState source)
    {
        if (villager.GatherSource == source.Id && villager.GatherPhase == GatherPhase.Gathering)
        {
            return;
        }

        villager.StartGathering(source.Id);

        // Taking more on top of a full load would carry past capacity and never deliver.
        if (villager.Load.Resource == source.Kind && villager.Load.IsFull)
        {
            CarryToDropOffPoint(state, villager);

            return;
        }

        WalkUpToSource(state.Map, villager, source);
    }

    /// <summary>
    /// Sends the Villager walking up to its source, to the free Cell beside it that has the
    /// shortest way to it; between Cells equally far, the one with the lowest index. A Villager
    /// that cannot reach any Cell beside the source walks to the Cell it can reach nearest to
    /// the source's and waits there, keeping its source, until a way opens.
    /// </summary>
    public static void WalkUpToSource(MapState map, UnitState villager, ResourceSourceState source)
    {
        villager.GatherPhase = GatherPhase.ToSource;
        MovementSystem.WalkToNearestOrTowards(map, villager, source.IsBeside, source.Cell);
    }

    /// <summary>
    /// Sends the Villager on the leg of the gather cycle it is walking, choosing again where it
    /// walks as it did when it set out: up to its source, or to a drop-off point with its load.
    /// False, changing nothing, when it walks no such leg.
    /// </summary>
    public static bool ChooseWayAgain(MatchState state, UnitState villager)
    {
        switch (villager.GatherPhase)
        {
            case GatherPhase.None:
            case GatherPhase.Gathering:
                return false;
            case GatherPhase.ToSource:
                WalkUpToSource(state.Map, villager, state.FindResourceSource(villager.GatherSource!.Value)!);
                return true;
            case GatherPhase.ToDropOffPoint:
                CarryToDropOffPoint(state, villager);
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(villager), villager.GatherPhase, "Unknown gather phase.");
        }
    }

    /// <summary>
    /// Sends every Villager carrying its load to a drop-off point that has left the match, on
    /// its way there or already waiting beside it, to the nearest one its Player still has, or
    /// leaves it idle with its load when there is none. It keeps its source.
    /// </summary>
    public static void RedirectCarriers(MatchState state)
    {
        foreach (var unit in state.Units)
        {
            if (unit.GatherPhase != GatherPhase.ToDropOffPoint)
            {
                continue;
            }

            // A carrier whose load filled beside a drop-off point waits there, with no path, for the next tick.
            var destination = unit.IsMoving ? unit.Path[^1] : unit.Position.Cell;

            if (!IsBesideDropOffPoint(state, unit.Owner, destination))
            {
                CarryToDropOffPoint(state, unit);
            }
        }
    }

    /// <summary>
    /// Sends the Villager walking to the free Cell beside a drop-off point of its Player that
    /// has the shortest way to it, whichever point that is; between Cells equally far, the one
    /// with the lowest index. A Player with no drop-off point leaves the Villager idle with its
    /// load. A Villager that cannot reach any Cell beside one walks to the Cell it can reach
    /// nearest to the centre of the drop-off point nearest to it in a straight line, and waits
    /// there with its load until a way opens.
    /// </summary>
    public static void CarryToDropOffPoint(MatchState state, UnitState villager)
    {
        var dropOffPoints = DropOffPointsOf(state, villager.Owner);

        if (dropOffPoints.Count == 0)
        {
            StandIdle(state.Map, villager);

            return;
        }

        villager.GatherPhase = GatherPhase.ToDropOffPoint;
        MovementSystem.WalkToNearestOrTowards(
            state.Map,
            villager,
            cell => dropOffPoints.Any(building => building.IsBeside(cell)),
            NearestCentre(dropOffPoints, villager.Position.Cell));
    }

    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var unit in state.Units)
        {
            switch (unit.GatherPhase)
            {
                case GatherPhase.None:
                    break;
                case GatherPhase.ToSource:
                    if (!unit.IsMoving)
                    {
                        ReachSource(state, unit);
                    }

                    break;
                case GatherPhase.Gathering:
                    TakeFromSource(context, state.FindResourceSource(unit.GatherSource!.Value)!, unit);
                    break;
                case GatherPhase.ToDropOffPoint:
                    if (!unit.IsMoving)
                    {
                        Deliver(state, unit);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit.GatherPhase, "Unknown gather phase.");
            }
        }
    }

    /// <summary>
    /// The Villager has walked as far as it can towards its source: beside it, it starts
    /// gathering; short of it, the source cannot be reached and the Villager waits.
    /// </summary>
    private static void ReachSource(MatchState state, UnitState villager)
    {
        if (state.FindResourceSource(villager.GatherSource!.Value)!.IsBeside(villager.Position.Cell))
        {
            villager.GatherPhase = GatherPhase.Gathering;
        }
    }

    private static void TakeFromSource(TickContext context, ResourceSourceState source, UnitState villager)
    {
        var state = context.State;

        if (!villager.GatherTick(Balance.Of(source.Kind).GatherTicksPerUnit))
        {
            return;
        }

        source.Amount--;
        // A load holds a single Resource: whatever else the Villager carried is dropped.
        var carried = villager.Load.Resource == source.Kind ? villager.Load.Amount : 0;
        villager.Load = new Load(source.Kind, carried + 1);

        if (villager.Load.IsFull)
        {
            CarryToDropOffPoint(state, villager);
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
        context.NoteWaysMayHaveOpened();
        context.Emit(new ResourceSourceDepleted(source.Id));

        var replacement = NearestSourceAround(state, source.Kind, source.Cell);

        foreach (var unit in state.Units)
        {
            if (unit.GatherSource != source.Id)
            {
                continue;
            }

            unit.ReplaceSource(replacement?.Id);

            if (unit.GatherPhase == GatherPhase.ToDropOffPoint)
            {
                continue;
            }

            if (replacement is null)
            {
                StandIdle(state.Map, unit);
            }
            else
            {
                GatherFrom(state, unit, replacement);
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
            var distance = source.Cell.SquaredDistanceTo(cell);

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
    private static void StandIdle(MapState map, UnitState villager)
    {
        villager.StopGathering();
        MovementSystem.WalkTo(map, villager, villager.Position.Cell);
    }

    /// <summary>
    /// The Villager has walked as far as it can towards a drop-off point: beside one of its
    /// Player's, it hands its load over and goes back to its source, or stands idle when it
    /// has none left; short of any, it waits with its load.
    /// </summary>
    private static void Deliver(MatchState state, UnitState villager)
    {
        if (!IsBesideDropOffPoint(state, villager.Owner, villager.Position.Cell))
        {
            return;
        }

        state.FindPlayer(villager.Owner)!.Receive(villager.Load.Resource, villager.Load.Amount);
        villager.Load = Load.Empty;

        if (villager.GatherSource is { } source)
        {
            GatherFrom(state, villager, state.FindResourceSource(source)!);
        }
        else
        {
            StandIdle(state.Map, villager);
        }
    }

    /// <summary>Whether the Cell lies beside one of the Player's drop-off points.</summary>
    private static bool IsBesideDropOffPoint(MatchState state, PlayerId owner, CellPosition cell) =>
        DropOffPointsOf(state, owner).Any(building => building.IsBeside(cell));

    /// <summary>
    /// The centre of the building whose centre is nearest to the Cell in a straight line.
    /// Between buildings equally near, the one first in the list: with the list in ascending
    /// ID order, the one with the lowest ID.
    /// </summary>
    private static CellPosition NearestCentre(List<BuildingState> buildings, CellPosition cell)
    {
        var nearest = buildings[0].Footprint.Centre;

        foreach (var building in buildings)
        {
            var centre = building.Footprint.Centre;

            if (centre.SquaredDistanceTo(cell) < nearest.SquaredDistanceTo(cell))
            {
                nearest = centre;
            }
        }

        return nearest;
    }

    /// <summary>The Player's drop-off points, in ascending ID order.</summary>
    private static List<BuildingState> DropOffPointsOf(MatchState state, PlayerId owner) =>
        state.Buildings.Where(building => building.Owner == owner && building.IsDropOffPoint).ToList();
}
