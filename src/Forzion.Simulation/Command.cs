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
