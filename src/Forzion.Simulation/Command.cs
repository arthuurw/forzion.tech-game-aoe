namespace Forzion.Simulation;

/// <summary>
/// An order issued by a Player, human or AI, and the only way to change a match from outside.
/// A command is enqueued with <see cref="Match.Enqueue"/> and applied by the next tick.
/// </summary>
/// <param name="Player">The Player issuing the command.</param>
public abstract record Command(PlayerId Player)
{
    /// <summary>
    /// Applies the command to the state. Called during a tick, after the match has checked
    /// that <paramref name="issuer"/> may still issue commands.
    /// </summary>
    internal abstract void Execute(TickContext context, PlayerState issuer);
}

/// <summary>
/// Sends units of the Player walking to a Cell. Each unit finds its own way around obstacles,
/// resource sources and buildings and stops on the centre of the Cell, or of the nearest Cell
/// it can reach when the destination itself cannot be reached. Moving calls off any attack
/// the units were making.
/// </summary>
/// <remarks>
/// The command is rejected as a whole, moving none of its units, when the destination is
/// outside the map or any of the units does not exist or belongs to another Player.
/// </remarks>
/// <param name="Units">The units to move.</param>
/// <param name="Destination">The Cell to walk to.</param>
public sealed record MoveCommand(PlayerId Player, IReadOnlyList<EntityId> Units, CellPosition Destination)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        var state = context.State;

        if (!state.Map.Contains(Destination))
        {
            context.Reject(this, RejectionReason.DestinationOutsideMap);

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
            unit.StopAttacking();
            MovementSystem.WalkTo(state.Map, unit, Destination);
        }
    }
}
