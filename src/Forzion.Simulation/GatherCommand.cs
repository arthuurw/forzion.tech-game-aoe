namespace Forzion.Simulation;

/// <summary>
/// Sends Villagers of the Player to gather from a resource source. Each Villager walks up to
/// the source and gathers from it until told otherwise, carrying its load to a drop-off point
/// whenever it is full. A Villager that was building stops building.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, sending none of its units, when the source is not in
/// the match, when any of the units belongs to another Player or is not a Villager, or when
/// none of the units exists. A unit that does not exist, because it died after the order was
/// given, is skipped and the others still set out.
/// </remarks>
/// <param name="Units">The Villagers to send.</param>
/// <param name="Source">The resource source to gather from.</param>
public sealed record GatherCommand(PlayerId Player, IReadOnlyList<EntityId> Units, EntityId Source)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var state = context.State;
        var source = state.FindResourceSource(Source);

        if (source is null)
        {
            context.Reject(this, RejectionReason.UnknownResourceSource);

            return;
        }

        var units = OrderedUnits.Find(context, this, issuer, Units);

        if (units is null)
        {
            return;
        }

        if (units.Any(unit => unit.Kind != UnitKind.Villager))
        {
            context.Reject(this, RejectionReason.UnitCannotGather);

            return;
        }

        foreach (var unit in units)
        {
            GatherSystem.GatherFrom(state, unit, source);
        }
    }
}
