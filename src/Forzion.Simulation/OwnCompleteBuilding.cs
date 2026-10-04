namespace Forzion.Simulation;

/// <summary>The check shared by every command that gives an order to one of the Player's complete buildings.</summary>
internal static class OwnCompleteBuilding
{
    /// <summary>
    /// The complete building of <paramref name="issuer"/> that <paramref name="id"/> names, or
    /// null once <paramref name="command"/> has been rejected: with
    /// <see cref="RejectionReason.UnknownBuilding"/> when the match has no such building, with
    /// <see cref="RejectionReason.BuildingOfAnotherPlayer"/> when it belongs to another Player,
    /// and with <see cref="RejectionReason.BuildingNotComplete"/> when it is a construction site.
    /// </summary>
    public static BuildingState? FindOrReject(TickContext context, Command command, PlayerState issuer, EntityId id)
    {
        var building = context.State.FindBuilding(id);

        if (building is null)
        {
            context.Reject(command, RejectionReason.UnknownBuilding);

            return null;
        }

        if (building.Owner != issuer.Id)
        {
            context.Reject(command, RejectionReason.BuildingOfAnotherPlayer);

            return null;
        }

        if (!building.IsComplete)
        {
            context.Reject(command, RejectionReason.BuildingNotComplete);

            return null;
        }

        return building;
    }
}
