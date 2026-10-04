namespace Forzion.Simulation;

/// <summary>
/// Starts the Age Advance of the Player at its Town Center: pays in full at once what the next
/// Age of its Faction costs, and the Player reaches that Age once the Age's advance time has
/// passed. The Town Center goes on training units meanwhile.
/// </summary>
/// <remarks>
/// The command is rejected, changing nothing, when the building is not in the match, belongs to
/// another Player, is a construction site or is not a Town Center, when an Age Advance of the
/// Player is already underway, when the Player is in the last Age of its Faction, or when the
/// Player cannot afford the cost. An Age Advance is not called off: it ends only when the
/// Player reaches the Age, or when the Town Center is destroyed, which takes the advance and its
/// cost with it.
/// </remarks>
/// <param name="Building">The Player's Town Center.</param>
public sealed record AgeAdvanceCommand(PlayerId Player, EntityId Building) : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        if (OwnCompleteBuilding.FindOrReject(context, this, issuer, Building) is not { } building)
        {
            return;
        }

        if (building.Kind != BuildingKind.TownCenter)
        {
            context.Reject(this, RejectionReason.BuildingCannotAdvanceAge);

            return;
        }

        if (context.State.Buildings.Any(each => each.Owner == issuer.Id && each.AgeAdvanceProgress is not null))
        {
            context.Reject(this, RejectionReason.AgeAdvanceInProgress);

            return;
        }

        if (issuer.Age >= issuer.Faction.Ages.Count)
        {
            context.Reject(this, RejectionReason.LastAgeReached);

            return;
        }

        // Ages are numbered from 1 and listed from index 0, so the next Age is at the current number.
        var cost = issuer.Faction.Ages[issuer.Age].AdvanceCost;

        if (!issuer.CanAfford(cost))
        {
            context.Reject(this, RejectionReason.NotEnoughResources);

            return;
        }

        issuer.Pay(cost);
        building.AgeAdvanceProgress = 0;
    }
}
