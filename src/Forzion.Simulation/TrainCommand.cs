namespace Forzion.Simulation;

/// <summary>
/// Puts a unit of the given kind at the end of the training queue of one of the Player's
/// complete buildings, paying its cost in full at once. The Town Center trains Villagers and
/// the Barracks military units.
/// </summary>
/// <remarks>
/// The command is rejected, queueing nothing and paying nothing, when the building is not in
/// the match, belongs to another Player or is a construction site, when the building does not
/// train units of that kind, when the Player's Age has not unlocked that kind in its Faction,
/// when the Player cannot afford the cost, or when the unit would
/// take the Player's population past its population limit. The unit takes its place in the
/// population as it joins the queue, so a limit that falls afterwards, when a House is
/// destroyed, does not stop the units already queued.
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

        if (!Enum.IsDefined(Kind) || Balance.Of(Kind).TrainedAt != building.Kind)
        {
            context.Reject(this, RejectionReason.BuildingCannotTrainUnit);

            return;
        }

        if (!issuer.Faction.Unlocks(Kind, issuer.Age))
        {
            context.Reject(this, RejectionReason.UnitLocked);

            return;
        }

        var cost = Balance.Of(Kind).Cost;

        if (!issuer.CanAfford(cost))
        {
            context.Reject(this, RejectionReason.NotEnoughResources);

            return;
        }

        var state = context.State;

        if (state.PopulationOf(issuer.Id) >= state.PopulationLimitOf(issuer.Id))
        {
            context.Reject(this, RejectionReason.PopulationLimitReached);

            return;
        }

        issuer.Pay(cost);
        building.QueueTraining(Kind);
    }
}
