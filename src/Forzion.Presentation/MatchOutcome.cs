using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>How the match ended for a Player, as the end screen shows it.</summary>
public enum MatchOutcome
{
    /// <summary>The Player is the winner.</summary>
    Victory,

    /// <summary>The Player was defeated, or the match ended without the Player winning it.</summary>
    Defeat,
}

/// <summary>Reads from the match how it ended for a Player.</summary>
public static class MatchOutcomes
{
    /// <summary>
    /// How the match ended for <paramref name="player"/>, or null while it goes on. A match
    /// has at most two Players and ends when at most one remains undefeated, so a Player's
    /// defeat always ends it.
    /// </summary>
    public static MatchOutcome? For(MatchState state, PlayerId player)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.IsOver)
        {
            return null;
        }

        return state.Winner == player ? MatchOutcome.Victory : MatchOutcome.Defeat;
    }
}
