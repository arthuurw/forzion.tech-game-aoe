namespace Forzion.Simulation;

/// <summary>
/// Places a construction site of the Player: a building of the given kind whose footprint
/// starts on <paramref name="Origin"/>. Its cost is paid in full at once, and its Cells are
/// blocked from then on.
/// </summary>
/// <remarks>
/// The command is rejected, placing nothing and paying nothing, when Players do not place
/// that kind of building, when a Cell of the footprint
/// is outside the map, is not free or has a unit standing on it, or when the Player cannot
/// afford the cost.
/// </remarks>
/// <param name="Kind">The kind of building to place.</param>
/// <param name="Origin">The Cell of the footprint with the lowest X and Y.</param>
public sealed record PlaceBuildingCommand(PlayerId Player, BuildingKind Kind, CellPosition Origin)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        // Each Player starts with its Town Center and never places another.
        if (Kind is not (BuildingKind.House or BuildingKind.Storehouse or BuildingKind.Barracks))
        {
            context.Reject(this, RejectionReason.BuildingNotPlaceable);

            return;
        }

        var size = Balance.BuildingSize(Kind);
        var cost = Balance.BuildingCost(Kind);

        if (!context.State.CanPlace(Kind, Origin))
        {
            context.Reject(this, RejectionReason.InvalidPlacement);

            return;
        }

        if (!issuer.CanAfford(cost))
        {
            context.Reject(this, RejectionReason.NotEnoughResources);

            return;
        }

        issuer.Pay(cost);
        context.State.AddBuilding(issuer.Id, Kind, Origin, size, size);
    }
}
