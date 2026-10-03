using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Tells the person at the screen when the match refuses one of their orders: a line of text
/// near the top of the screen that fades after a few seconds. The texts come from the
/// project's translations (<c>translations/pt_BR.po</c>), looked up by the keys
/// <see cref="RejectionNotices"/> names.
/// </summary>
public partial class RejectionNoticeBoard : CanvasLayer
{
    private const int MostShown = 4;

    private VBoxContainer lines = null!;

    /// <summary>The match whose refusals are reported.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>Seconds a notice stays on screen, its fade included.</summary>
    [Export]
    public double SecondsShown { get; set; } = 4;

    public override void _Ready()
    {
        lines = new VBoxContainer
        {
            Name = "Notices",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        lines.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        lines.Position = new Vector2(lines.Position.X, 64);
        lines.GrowHorizontal = Control.GrowDirection.Both;
        AddChild(lines);

        MatchView.EventsHappened += Show;
    }

    public override void _ExitTree() => MatchView.EventsHappened -= Show;

    private void Show(IReadOnlyList<MatchEvent> events)
    {
        foreach (var key in RejectionNotices.MessageKeysFor(events, MatchView.HumanPlayer))
        {
            AddNotice(Tr(key));
        }
    }

    private void AddNotice(string text)
    {
        // The oldest notice gives way when too many pile up.
        if (lines.GetChildCount() >= MostShown)
        {
            var oldest = lines.GetChild(0);
            lines.RemoveChild(oldest);
            oldest.QueueFree();
        }

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.4f));
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        label.AddThemeConstantOverride("outline_size", 6);
        lines.AddChild(label);

        // Shown whole for most of its time, then faded out and removed.
        var fade = Math.Min(1, SecondsShown / 2);
        var tween = label.CreateTween();
        tween.TweenInterval(SecondsShown - fade);
        tween.TweenProperty(label, "modulate:a", 0.0, fade);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }
}
