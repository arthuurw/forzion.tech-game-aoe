using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The mouse of the person at the screen: the left button selects by click or by a dragged
/// box, the right button orders the selected units or sets the selected building's rally
/// point. While a building chosen in the HUD is being placed, the left button places it and
/// the right button or Escape gives up. What a gesture selects and which command it sends is
/// decided by <see cref="PlayerControl"/>, outside the engine; this node only feeds it the
/// mouse and the camera's lines of sight, and draws the box being dragged.
/// </summary>
/// <remarks>
/// Bindings live in the project's input map: the <c>select</c> and <c>order</c> actions, and
/// Godot's built-in <c>ui_cancel</c>. Clicks on the HUD never reach this node, and nothing
/// does while the match is paused.
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
    public PlayerControl PlayerControl { get; private set; } = null!;

    public override void _Ready()
    {
        PlayerControl = new PlayerControl(MatchView.Driver, MatchView.HumanPlayer, SightThrough, Placeholders.PickSizes);

        box = new Panel
        {
            Name = "DragBox",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
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
        // While paused no order reaches the match, and Escape is left to the pause screen to
        // resume. A box being dragged is dropped.
        if (MatchView.Driver.IsPaused)
        {
            pressedAt = null;
            box.Visible = false;

            return;
        }

        if (PlayerControl.PlacingBuilding is not null && @event.IsActionPressed("ui_cancel"))
        {
            PlayerControl.CancelPlacement();
            GetViewport().SetInputAsHandled();

            return;
        }

        if (@event is not InputEventMouse mouse)
        {
            return;
        }

        var at = new ScreenPoint(mouse.Position.X, mouse.Position.Y);

        // While a building is being placed, the left button places it and the right gives up;
        // neither selects nor orders.
        if (PlayerControl.PlacingBuilding is not null)
        {
            if (@event.IsActionPressed("select"))
            {
                PlayerControl.PlaceAt(at);
            }
            else if (@event.IsActionPressed("order"))
            {
                PlayerControl.CancelPlacement();
            }

            return;
        }

        if (@event.IsActionPressed("select"))
        {
            pressedAt = at;
        }
        else if (@event.IsActionReleased("select") && pressedAt is { } from)
        {
            PlayerControl.Select(from, at);
            pressedAt = null;
            box.Visible = false;
        }
        else if (@event.IsActionPressed("order"))
        {
            PlayerControl.OrderAt(at);
        }
        else if (@event is InputEventMouseMotion && pressedAt is { } start)
        {
            ShowBox(start, at);
        }
    }

    private void ShowBox(ScreenPoint from, ScreenPoint to)
    {
        box.Visible = !PlayerControl.IsClick(from, to);

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

        return SightLine.FromRay(
            new WorldVector(origin.X, origin.Y, origin.Z), new WorldVector(direction.X, direction.Y, direction.Z));
    }
}
