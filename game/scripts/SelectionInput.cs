using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The mouse of the person at the screen: the left button selects by click or by a dragged
/// box, the right button orders the selected units. What a gesture selects and which command
/// it sends is decided by <see cref="PlayerControl"/>, outside the engine; this node only
/// feeds it the mouse and the camera's lines of sight, and draws the box being dragged.
/// </summary>
/// <remarks>
/// Bindings live in the project's input map: the <c>select</c> and <c>order</c> actions.
/// </remarks>
public partial class SelectionInput : CanvasLayer
{
    private Panel box = null!;
    private ScreenPoint? pressedAt;

    /// <summary>The match whose units are selected and ordered.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The camera the person looks through.</summary>
    [Export]
    public Camera3D Camera { get; set; } = null!;

    /// <summary>The human Player's selection and orders. Set in <see cref="_Ready"/>.</summary>
    public PlayerControl Control { get; private set; } = null!;

    public override void _Ready()
    {
        Control = new PlayerControl(MatchView.Driver, MatchView.HumanPlayer, SightThrough, Placeholders.PickSizes);

        box = new Panel
        {
            Name = "DragBox",
            Visible = false,
            MouseFilter = Godot.Control.MouseFilterEnum.Ignore,
        };
        box.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1, 1, 1, 0.12f),
            BorderColor = new Color(1, 1, 1, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
        });
        AddChild(box);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouse mouse)
        {
            return;
        }

        var at = new ScreenPoint(mouse.Position.X, mouse.Position.Y);

        if (@event.IsActionPressed("select"))
        {
            pressedAt = at;
        }
        else if (@event.IsActionReleased("select") && pressedAt is { } from)
        {
            Control.Select(from, at);
            pressedAt = null;
            box.Visible = false;
        }
        else if (@event.IsActionPressed("order"))
        {
            Control.OrderAt(at);
        }
        else if (@event is InputEventMouseMotion && pressedAt is { } start)
        {
            ShowBox(start, at);
        }
    }

    private void ShowBox(ScreenPoint from, ScreenPoint to)
    {
        box.Visible = !Control.IsClick(from, to);

        var corner = new Vector2((float)Math.Min(from.X, to.X), (float)Math.Min(from.Y, to.Y));
        var size = new Vector2((float)Math.Abs(to.X - from.X), (float)Math.Abs(to.Y - from.Y));

        box.Position = corner;
        box.Size = size;
    }

    /// <summary>
    /// The camera's line of sight through a point of the viewport, in map coordinates; null
    /// when it looks at or above the horizon and never meets the ground.
    /// </summary>
    private SightLine? SightThrough(ScreenPoint point)
    {
        var screen = new Vector2((float)point.X, (float)point.Y);
        var origin = Camera.ProjectRayOrigin(screen);
        var direction = Camera.ProjectRayNormal(screen);

        if (direction.Y >= -1e-4f)
        {
            return null;
        }

        // Along the ray the height changes by direction.Y per step: dividing by it gives the
        // step that changes the height by one unit.
        var perHeight = direction / direction.Y;
        var ground = origin - (perHeight * origin.Y);

        return new SightLine(new MapPoint(ground.X, ground.Z), new MapPoint(perHeight.X, perHeight.Z));
    }
}
