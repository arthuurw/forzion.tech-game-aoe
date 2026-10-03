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

/// <summary>The Player gives up the match and is defeated.</summary>
public sealed record ResignCommand(PlayerId Player) : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        issuer.IsDefeated = true;
        context.Emit(new PlayerDefeated(issuer.Id));
    }
}

/// <summary>
/// Sends units of the Player walking to a Cell. Each unit finds its own way around obstacles,
/// resource sources and buildings and stops on the centre of the Cell.
/// </summary>
/// <param name="Units">The units to move.</param>
/// <param name="Destination">The Cell to walk to.</param>
public sealed record MoveCommand(PlayerId Player, IReadOnlyList<EntityId> Units, CellPosition Destination)
    : Command(Player)
{
    internal override void Execute(TickContext context, PlayerState issuer)
    {
        foreach (var id in Units)
        {
            if (context.State.FindUnit(id) is { } unit)
            {
                MovementSystem.WalkTo(context.State.Map, unit, Destination);
            }
        }
    }
}
