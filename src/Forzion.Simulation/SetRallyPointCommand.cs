namespace Forzion.Simulation;

/// <summary>
/// Sets the rally point of one of the Player's buildings that train units: the Cell the units
/// it trains from then on walk to on their own. Units already trained are not called back.
/// </summary>
/// <remarks>
/// The command is rejected, changing nothing, when the Cell is outside the map, when the
/// building is not in the match, belongs to another Player or is a construction site, or when
/// it trains no units. A Cell units cannot reach is accepted: they walk to the nearest Cell they can.
/// </remarks>
/// <param name="Building">The building whose rally point to set.</param>
/// <param name="Cell">The Cell its units are to walk to.</param>
public sealed record SetRallyPointCommand(PlayerId Player, EntityId Building, CellPosition Cell) : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        if (!context.State.Map.Contains(Cell))
        {
            context.Reject(this, RejectionReason.DestinationOutsideMap);

            return;
        }

        if (ProductionBuilding.Find(context, this, issuer, Building) is not { } building)
        {
            return;
        }

        if (!Balance.Of(building.Kind).Trains)
        {
            context.Reject(this, RejectionReason.BuildingCannotTrain);

            return;
        }

        building.RallyPoint = Cell;
    }
}
