using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The end screen: once the match is over for the human Player, a veil over the match with the
/// outcome that <see cref="MatchOutcomes"/> reads, a victory or a defeat, how it came about, a
/// button that plays the same skirmish again and one that leaves for the main menu. The match
/// goes on under the veil; nothing reaches it from the mouse any more.
/// </summary>
public partial class EndScreen : CanvasLayer
{
    private static readonly Color VictoryColour = new(1f, 0.85f, 0.35f);
    private static readonly Color DefeatColour = new(0.95f, 0.35f, 0.3f);

    private bool shown;

    /// <summary>The match whose end is shown.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    public override void _Process(double delta)
    {
        if (!shown && MatchOutcomes.For(MatchView.State, MatchView.HumanPlayer) is { } outcome)
        {
            ShowOutcome(outcome);
        }
    }

    private void ShowOutcome(MatchOutcome outcome)
    {
        var colour = outcome switch
        {
            MatchOutcome.Victory => VictoryColour,
            MatchOutcome.Defeat => DefeatColour,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown outcome."),
        };

        var playAgain = Overlays.NewButton(Tr(ScreenTexts.PlayAgain));
        playAgain.Name = "PlayAgain";
        playAgain.Pressed += () => GetTree().ReloadCurrentScene();

        AddChild(Overlays.NewScreenOverMatch(
            this,
            Overlays.NewHeading(Tr(ScreenTexts.TitleOf(outcome)), colour),
            Overlays.NewText(Tr(ScreenTexts.ReasonOf(outcome)), 24, Palette.Heading),
            playAgain));
        shown = true;
        playAgain.GrabFocus();
    }
}
