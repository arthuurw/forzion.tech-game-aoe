using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The pause: the match stops, through <see cref="MatchDriver.IsPaused"/>, under a veil with a
/// button to go on and one to leave for the main menu. The camera still moves meanwhile. A
/// match that is over no longer pauses: the end screen has taken over.
/// </summary>
/// <remarks>
/// The <c>pause</c> action of the project's input map toggles it: Escape, P or the Pause key.
/// Godot hands unhandled input to the last node of the scene first, and this node comes
/// before <see cref="SelectionInput"/>, so Escape first gives up a building being placed and
/// pauses only when nothing is being placed.
/// </remarks>
public partial class PauseMenu : CanvasLayer
{
    private ColorRect veil = null!;
    private Button resume = null!;

    /// <summary>The match that pauses.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    public override void _Ready()
    {
        resume = Overlays.NewButton(Tr(ScreenTexts.Resume));
        resume.Name = "Resume";
        resume.Pressed += Resume;

        var mainMenu = Overlays.NewButton(Tr(ScreenTexts.MainMenu));
        mainMenu.Name = "MainMenu";
        mainMenu.Pressed += () => GetTree().ChangeSceneToFile(Scenes.MainMenu);

        veil = Overlays.NewVeil();
        veil.Visible = false;
        veil.AddChild(Overlays.NewColumn(Overlays.NewHeading(Tr(ScreenTexts.Paused), Overlays.TitleColour), resume, mainMenu));
        AddChild(veil);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("pause"))
        {
            return;
        }

        if (MatchView.Driver.IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }

        GetViewport().SetInputAsHandled();
    }

    /// <summary>Stops the match and shows the pause, unless the match is over.</summary>
    public void Pause()
    {
        if (MatchView.State.IsOver)
        {
            return;
        }

        MatchView.Driver.IsPaused = true;
        veil.Visible = true;
        resume.GrabFocus();
    }

    /// <summary>Hides the pause and lets the match go on.</summary>
    public void Resume()
    {
        MatchView.Driver.IsPaused = false;
        veil.Visible = false;
    }
}
