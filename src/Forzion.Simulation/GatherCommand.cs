namespace Forzion.Simulation;

/// <summary>
/// Sends Villagers of the Player to gather from a resource source. Each Villager walks up to
/// the source and gathers from it until told otherwise, carrying its load to a drop-off point
/// whenever it is full.
/// </summary>
/// <param name="Units">The Villagers to send.</param>
/// <param name="Source">The resource source to gather from.</param>
public sealed record GatherCommand(PlayerId Player, IReadOnlyList<EntityId> Units, EntityId Source)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var state = context.State;
        var source = state.FindResourceSource(Source)!;

        foreach (var id in Units)
        {
            GatherSystem.GatherFrom(state.Map, state.FindUnit(id)!, source);
        }
    }
}
