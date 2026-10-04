using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>Finds the Player whose part of the match the HUD shows.</summary>
internal static class PlayerLookup
{
    /// <summary>The state of <paramref name="player"/> in the current tick of the match.</summary>
    /// <exception cref="ArgumentException">The match has no such Player.</exception>
    public static PlayerState Find(MatchState state, PlayerId player) =>
        state.Players.FirstOrDefault(each => each.Id == player)
            ?? throw new ArgumentException($"The match has no Player {player.Value}.", nameof(player));
}
