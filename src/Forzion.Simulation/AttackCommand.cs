namespace Forzion.Simulation;

/// <summary>
/// Sends units of the Player to attack a unit or building of another Player. Each unit closes
/// in until the target is within its range and hits it until it is destroyed.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, sending none of its units, when the target is not a
/// unit or building of another Player, or any of the units does not exist, belongs to another
/// Player or cannot attack.
/// </remarks>
/// <param name="Units">The units to attack with.</param>
/// <param name="Target">The unit or building to attack.</param>
public sealed record AttackCommand(PlayerId Player, IReadOnlyList<EntityId> Units, EntityId Target)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var state = context.State;
        PlayerId? targetOwner = state.FindUnit(Target)?.Owner ?? state.FindBuilding(Target)?.Owner;

        if (targetOwner is null)
        {
            context.Reject(this, RejectionReason.UnknownTarget);

            return;
        }

        if (targetOwner == issuer.Id)
        {
            context.Reject(this, RejectionReason.OwnTarget);

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

            if (Balance.Attack(unit.Kind) is null)
            {
                context.Reject(this, RejectionReason.UnitCannotAttack);

                return;
            }

            units.Add(unit);
        }

        foreach (var unit in units)
        {
            unit.Attack(Target);
        }
    }
}
