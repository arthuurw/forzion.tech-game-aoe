namespace Forzion.Simulation;

/// <summary>
/// Puts a unit of the given kind at the end of the training queue of one of the Player's
/// complete buildings, paying its cost in full at once. The Town Center trains Villagers and
/// the Barracks military units.
/// </summary>
/// <remarks>
/// The command is rejected, queueing nothing and paying nothing, when the building is not in
/// the match, belongs to another Player or is a construction site, when the building does not
/// train units of that kind, or when the Player cannot afford the cost.
/// </remarks>
/// <param name="Building">The building to train the unit.</param>
/// <param name="Kind">The kind of unit to train.</param>
public sealed record TrainCommand(PlayerId Player, EntityId Building, UnitKind Kind) : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        if (ProductionBuilding.Find(context, this, issuer, Building) is not { } building)
        {
            return;
        }

        if (!Enum.IsDefined(Kind) || Balance.TrainedAt(Kind) != building.Kind)
        {
            context.Reject(this, RejectionReason.BuildingCannotTrainUnit);

            return;
        }

        var cost = Balance.UnitCost(Kind);

        if (!issuer.CanAfford(cost))
        {
            context.Reject(this, RejectionReason.NotEnoughResources);

            return;
        }

        issuer.Pay(cost);
        building.QueueTraining(Kind);
    }
}
