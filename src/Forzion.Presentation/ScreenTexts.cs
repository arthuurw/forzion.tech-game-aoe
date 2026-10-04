namespace Forzion.Presentation;

/// <summary>
/// Keys of the texts of the screens around a match: the main menu, the pause and the end
/// screen. The words live in the game's translations.
/// </summary>
public static class ScreenTexts
{
    /// <summary>The name of the game, over the main menu.</summary>
    public const string GameTitle = "GAME_TITLE";

    /// <summary>The main menu's button that starts a skirmish against the AI.</summary>
    public const string Skirmish = "MENU_SKIRMISH";

    /// <summary>The main menu's button that closes the game.</summary>
    public const string Quit = "MENU_QUIT";

    /// <summary>The heading of the pause.</summary>
    public const string Paused = "PAUSE_TITLE";

    /// <summary>The button that ends the pause and lets the match go on.</summary>
    public const string Resume = "PAUSE_RESUME";

    /// <summary>The button that leaves the match for the main menu, from the pause or the end screen.</summary>
    public const string MainMenu = "SCREEN_MAIN_MENU";

    /// <summary>The end screen's button that starts the same skirmish anew.</summary>
    public const string PlayAgain = "END_PLAY_AGAIN";

    /// <summary>The heading of the end screen after a victory.</summary>
    public const string Victory = "END_VICTORY";

    /// <summary>Under the heading of a victory, how it was won.</summary>
    public const string VictoryReason = "END_VICTORY_REASON";

    /// <summary>The heading of the end screen after a defeat.</summary>
    public const string Defeat = "END_DEFEAT";

    /// <summary>Under the heading of a defeat, how it was lost.</summary>
    public const string DefeatReason = "END_DEFEAT_REASON";

    /// <summary>The key of the end screen's heading for the outcome.</summary>
    public static string TitleOf(MatchOutcome outcome) => outcome switch
    {
        MatchOutcome.Victory => Victory,
        MatchOutcome.Defeat => Defeat,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome."),
    };

    /// <summary>
    /// The key of the line under the end screen's heading, which says how the match was won or
    /// lost: a match is lost only with the Player's own Town Center, and won only with the
    /// enemy's.
    /// </summary>
    public static string ReasonOf(MatchOutcome outcome) => outcome switch
    {
        MatchOutcome.Victory => VictoryReason,
        MatchOutcome.Defeat => DefeatReason,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome."),
    };
}
