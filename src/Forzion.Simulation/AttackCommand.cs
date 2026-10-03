namespace Forzion.Simulation;

/// <summary>
/// Sends units of the Player to attack a unit or building of another Player. Each unit closes
/// in until the target is within its range and hits it until it is destroyed.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, sending none of its units, when the target is not a
/// unit or building of another Player, when any of the units belongs to another Player or
/// cannot attack, or when none of the units exists. A unit that does not exist, because it
/// died after the order was given, is skipped and the others still attack.
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

        var units = OrderedUnits.Find(context, this, issuer, Units);

        if (units is null)
        {
            return;
        }

        if (units.Any(unit => Balance.Attack(unit.Kind) is null))
        {
            context.Reject(this, RejectionReason.UnitCannotAttack);

            return;
        }

        foreach (var unit in units)
        {
            unit.Attack(Target);
        }
    }
}
