namespace Forzion.Simulation;

/// <summary>
/// Sends Villagers of the Player to build one of its construction sites. Each Villager walks
/// up to the site, stopping whatever gathering it was doing, and builds it until it is complete.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, sending none of its units, when any of the units
/// belongs to another Player, when none of them exists, or when the building is not in the
/// match, belongs to another Player or is already complete. A unit that does not exist,
/// because it died after the order was given, is skipped and the others still set out.
/// </remarks>
/// <param name="Units">The Villagers to send.</param>
/// <param name="Building">The construction site to build.</param>
public sealed record BuildCommand(PlayerId Player, IReadOnlyList<EntityId> Units, EntityId Building)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var units = OrderedUnits.Find(context, this, issuer, Units);

        if (units is null)
        {
            return;
        }

        var site = context.State.FindBuilding(Building);

        if (site is null)
        {
            context.Reject(this, RejectionReason.UnknownBuilding);

            return;
        }

        if (site.Owner != issuer.Id)
        {
            context.Reject(this, RejectionReason.BuildingOfAnotherPlayer);

            return;
        }

        if (site.IsComplete)
        {
            context.Reject(this, RejectionReason.BuildingAlreadyComplete);

            return;
        }

        foreach (var unit in units)
        {
            ConstructionSystem.Build(context.State.Map, unit, site);
        }
    }
}
