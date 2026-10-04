namespace Forzion.Simulation;

/// <summary>
/// Moves every Age Advance underway on by one tick, and once the advance time of the next Age
/// of the Player's Faction has passed, puts the Player in that Age. Buildings are visited in
/// ascending ID order, so the Players advancing in the same tick are reported in that order.
/// </summary>
internal sealed class AgeAdvanceSystem : ISystem
{
    public void Run(TickContext context)
    {
        foreach (var building in context.State.Buildings)
        {
            if (building.AgeAdvanceProgress is not { } progress)
            {
                continue;
            }

            var player = context.State.FindPlayer(building.Owner)!;
            var advanceTime = player.NextAge!.AdvanceTime;

            // The tick that applies the order counts as the first of the advance, as it does for
            // training, so an advance time of zero or one is done in that very tick.
            if (progress + 1 < advanceTime)
            {
                building.AgeAdvanceProgress = progress + 1;

                continue;
            }

            player.Age++;
            building.AgeAdvanceProgress = null;
            context.Emit(new AgeAdvanced(player.Id, player.Age));
        }
    }
}
