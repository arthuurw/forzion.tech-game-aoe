namespace Forzion.Simulation;

/// <summary>
/// Places a construction site of the Player: a building of the given kind whose footprint
/// starts on <paramref name="Origin"/>. Its cost is paid in full at once, and its Cells are
/// blocked from then on: units on their way across them find another way. The builders walk
/// up to the site and build it, stopping whatever gathering they were doing.
/// </summary>
/// <remarks>
/// The command is rejected, placing nothing and paying nothing, when a builder belongs to
/// another Player or is not a Villager, when builders are named and none of them exists,
/// when Players do not place that kind of building, when the Player's Age has not unlocked
/// that kind in its Faction, when a Cell of the footprint is outside
/// the map, is not free or has a unit standing on it, or when the Player cannot afford the
/// cost. A builder that does not exist, because it died after the order was given, is skipped.
/// </remarks>
/// <param name="Kind">The kind of building to place.</param>
/// <param name="Origin">The Cell of the footprint with the lowest X and Y.</param>
/// <param name="Builders">The Villagers sent to build the site. May be empty.</param>
public sealed record PlaceBuildingCommand(
    PlayerId Player, BuildingKind Kind, CellPosition Origin, IReadOnlyList<EntityId> Builders)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        // No builder is a valid order: the site waits for a later build order.
        var builders = Builders.Count == 0 ? [] : OrderedUnits.Find(context, this, issuer, Builders);

        if (builders is null)
        {
            return;
        }

        if (builders.Any(builder => builder.Kind != UnitKind.Villager))
        {
            context.Reject(this, RejectionReason.UnitCannotBuild);

            return;
        }

        // Each Player starts with its Town Center and never places another.
        if (Kind is not (BuildingKind.House or BuildingKind.Storehouse or BuildingKind.Barracks))
        {
            context.Reject(this, RejectionReason.BuildingNotPlaceable);

            return;
        }

        if (!issuer.Faction.Unlocks(Kind, issuer.Age))
        {
            context.Reject(this, RejectionReason.BuildingLocked);

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
        var site = context.State.AddBuilding(issuer.Id, Kind, Origin, size, size);
        MovementSystem.Reroute(context.State);

        foreach (var builder in builders)
        {
            ConstructionSystem.Build(context.State.Map, builder, site);
        }
    }
}
