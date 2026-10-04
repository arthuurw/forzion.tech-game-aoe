using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The main menu, the first screen of the game: the game's name, a button that starts a
/// skirmish against the AI and one that closes the game. Texts come from the project's
/// translations.
/// </summary>
public partial class MainMenu : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect { Name = "Background", Color = Overlays.Backdrop };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var skirmish = Overlays.NewButton(Tr(ScreenTexts.Skirmish));
        skirmish.Name = "Skirmish";
        skirmish.Pressed += () => GetTree().ChangeSceneToFile(Scenes.Skirmish);

        var quit = Overlays.NewButton(Tr(ScreenTexts.Quit));
        quit.Name = "Quit";
        quit.Pressed += () => GetTree().Quit();

        AddChild(Overlays.NewColumn(Overlays.NewHeading(Tr(ScreenTexts.GameTitle), Overlays.TitleColour), skirmish, quit));
        skirmish.GrabFocus();
    }
}
