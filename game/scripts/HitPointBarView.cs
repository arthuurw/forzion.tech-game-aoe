using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Draws the hit point bars <see cref="HitPointBars"/> picks: a short bar on screen just above
/// each selected or wounded unit and building, filled to the share of hit points it has left.
/// </summary>
public partial class HitPointBarView : CanvasLayer
{
    private const float BarThickness = 5;
    private const float UnitBarWidth = 36;
    private const float BuildingBarWidth = 72;

    // How far above the top of an entity's shape the bar floats, in world units.
    private const float Clearance = 0.35f;

    private static readonly Color Background = new(0.1f, 0.1f, 0.1f, 0.8f);
    private static readonly Color Whole = new(0.3f, 0.85f, 0.3f);
    private static readonly Color Hurt = new(0.95f, 0.8f, 0.2f);
    private static readonly Color Dying = new(0.9f, 0.2f, 0.15f);

    private Control canvas = null!;

    /// <summary>The match whose units and buildings the bars are drawn over.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The input whose selected entities show their bars.</summary>
    [Export]
    public SelectionInput SelectionInput { get; set; } = null!;

    /// <summary>The camera the bars are drawn for.</summary>
    [Export]
    public Camera3D Camera { get; set; } = null!;

    public override void _Ready()
    {
        canvas = new Control
        {
            Name = "Bars",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        canvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvas.Draw += DrawBars;
        AddChild(canvas);
    }

    public override void _Process(double delta) => canvas.QueueRedraw();

    private void DrawBars()
    {
        foreach (var bar in HitPointBars.Shown(MatchView.Driver, SelectionInput.Control.Selected))
        {
            var top = bar.OverBuilding ? Placeholders.BuildingTop : Placeholders.UnitTop;
            var above = WorldSpace.ToWorld(bar.Position, top + Clearance);

            if (Camera.IsPositionBehind(above))
            {
                continue;
            }

            var width = bar.OverBuilding ? BuildingBarWidth : UnitBarWidth;
            var centre = Camera.UnprojectPosition(above);
            var corner = centre - new Vector2(width / 2, BarThickness / 2);

            canvas.DrawRect(new Rect2(corner, new Vector2(width, BarThickness)), Background);
            canvas.DrawRect(new Rect2(corner, new Vector2(width * (float)bar.Fill, BarThickness)), ColourOf(bar.Fill));
        }
    }

    private static Color ColourOf(double fill) => fill switch
    {
        > 0.5 => Whole,
        > 0.25 => Hurt,
        _ => Dying,
    };
}
