namespace Forzion.Simulation;

/// <summary>
/// Defeats every Player left without a Town Center and ends the match once at most one
/// Player remains undefeated. A match of a single Player never ends this way.
/// </summary>
internal sealed class DefeatSystem : ISystem
{
    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var player in state.Players)
        {
            if (!player.IsDefeated
                && !state.Buildings.Any(building => building.Owner == player.Id && building.Kind == BuildingKind.TownCenter))
            {
                player.IsDefeated = true;
                context.Emit(new PlayerDefeated(player.Id));
            }
        }

        var remaining = state.Players.Where(player => !player.IsDefeated).ToList();

        if (!state.IsOver && state.Players.Count > 1 && remaining.Count <= 1)
        {
            state.End(remaining.Count == 1 ? remaining[0].Id : null);
            context.Emit(new MatchEnded(state.Winner));
        }
    }
}
