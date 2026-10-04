namespace Forzion.Simulation;

/// <summary>
/// Sends units of the Player to attack a unit or building of another Player. Each unit closes
/// in until the target is within its range and hits it until it is destroyed.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, sending none of its units, when the target is not a
/// unit or building of another Player, when any of the units belongs to another Player, when
/// none of the units exists or when none of them can attack. A unit that does not exist,
/// because it died after the order was given, is skipped and the others still attack; so is
/// a unit that cannot attack, such as a Villager, which goes on with whatever it was doing.
/// </remarks>
/// <param name="Units">The units to attack with.</param>
/// <param name="Target">The unit or building to attack.</param>
public sealed record AttackCommand(PlayerId Player, IReadOnlyList<EntityId> Units, EntityId Target)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var state = context.State;
        var (targetUnit, targetBuilding) = state.FindUnitOrBuilding(Target);
        var targetOwner = targetUnit?.Owner ?? targetBuilding?.Owner;

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

        var units = CommandedUnits.Find(
            context, this, issuer, Units, unit => unit.CanAttack, RejectionReason.UnitCannotAttack);

        if (units is null)
        {
            return;
        }

        foreach (var unit in units)
        {
            unit.Attack(Target);
        }
    }
}
