namespace Forzion.Simulation;

/// <summary>
/// Sends units of the Player to attack a unit or building of another Player. Each unit closes
/// in until the target is within its range and hits it until it is destroyed.
/// </summary>
/// <param name="Units">The units to attack with.</param>
/// <param name="Target">The unit or building to attack.</param>
public sealed record AttackCommand(PlayerId Player, IReadOnlyList<EntityId> Units, EntityId Target)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        foreach (var id in Units)
        {
            context.State.FindUnit(id)?.Attack(Target);
        }
    }
}
