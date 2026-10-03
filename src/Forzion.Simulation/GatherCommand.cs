namespace Forzion.Simulation;

/// <summary>
/// Sends Villagers of the Player to gather from a resource source. Each Villager walks up to
/// the source and gathers from it until told otherwise, carrying its load to a drop-off point
/// whenever it is full.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, sending none of its units, when the source is not in
/// the match or any of the units does not exist or belongs to another Player.
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

        var units = new List<UnitState>(Units.Count);

        foreach (var id in Units)
        {
            var unit = state.FindUnit(id);

            if (unit is null)
            {
                context.Reject(this, RejectionReason.UnknownUnit);

                return;
            }

            if (unit.Owner != issuer.Id)
            {
                context.Reject(this, RejectionReason.UnitOfAnotherPlayer);

                return;
            }

            units.Add(unit);
        }

        foreach (var unit in units)
        {
            GatherSystem.GatherFrom(state.Map, unit, source);
        }
    }
}
