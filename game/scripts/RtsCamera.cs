using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// RTS camera: a perspective camera at a fixed angle looking down at a point on the ground.
/// The point pans with the keyboard or with the mouse at the edge of the screen and stays on
/// the map; the camera zooms by moving closer to it or farther from it, within limits.
/// </summary>
/// <remarks>
/// This node is the point on the ground; its child <see cref="Camera3D"/> named "Camera" is
/// placed behind and above it. Bindings live in the project's input map: the
/// <c>camera_pan_*</c> and <c>camera_zoom_*</c> actions.
/// </remarks>
public partial class RtsCamera : Node3D
{
    private Camera3D camera = null!;
    private float distance;
    private float targetDistance;
    private bool mouseInWindow;

    /// <summary>The match whose map bounds the camera and whose human Player's Town Center it starts on.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>How far the camera looks down, in degrees below the horizon.</summary>
    [Export(PropertyHint.Range, "20,89")]
    public float PitchDegrees { get; set; } = 55;

    /// <summary>Closest the camera gets to the ground point, in world units.</summary>
    [Export]
    public float MinDistance { get; set; } = 8;

    /// <summary>Farthest the camera gets from the ground point, in world units.</summary>
    [Export]
    public float MaxDistance { get; set; } = 60;

    /// <summary>Distance from the ground point when the match starts, in world units.</summary>
    [Export]
    public float StartDistance { get; set; } = 24;

    /// <summary>Factor one zoom step multiplies or divides the distance by.</summary>
    [Export]
    public float ZoomStep { get; set; } = 1.15f;

    /// <summary>How fast the distance closes on the zoom target, per second. Higher is snappier.</summary>
    [Export]
    public float ZoomSmoothing { get; set; } = 12;

    /// <summary>Pan speed in world units per second for each unit of distance, so the screen moves at the same pace at any zoom.</summary>
    [Export]
    public float PanSpeed { get; set; } = 1;

    /// <summary>Width in pixels of the band along the edges of the screen where the mouse pans.</summary>
    [Export]
    public int EdgeMargin { get; set; } = 8;

    public override void _Ready()
    {
        camera = GetNode<Camera3D>("Camera");
        distance = targetDistance = Mathf.Clamp(StartDistance, MinDistance, MaxDistance);

        var homeTownCenter = MatchView.State.Buildings
            .FirstOrDefault(building => building.Owner == MatchView.HumanPlayer && building.Kind == BuildingKind.TownCenter);

        Position = homeTownCenter is null ? MapCentre() : WorldSpace.CentreOf(homeTownCenter);
        PlaceCamera();
    }

    public override void _Notification(int what)
    {
        // Edge pan only while the mouse is in the window: once it leaves, the last position
        // Godot reports stays at the edge and would pan forever.
        if (what == NotificationWMMouseEnter)
        {
            mouseInWindow = true;
        }
        else if (what == NotificationWMMouseExit)
        {
            mouseInWindow = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("camera_zoom_in", allowEcho: true))
        {
            targetDistance = Mathf.Max(targetDistance / ZoomStep, MinDistance);
        }
        else if (@event.IsActionPressed("camera_zoom_out", allowEcho: true))
        {
            targetDistance = Mathf.Min(targetDistance * ZoomStep, MaxDistance);
        }
    }

    public override void _Process(double delta)
    {
        var seconds = (float)delta;

        var direction = Input.GetVector("camera_pan_left", "camera_pan_right", "camera_pan_up", "camera_pan_down") + EdgeDirection();
        direction = direction.LimitLength(1);

        var map = MatchView.State.Map;
        var moved = Position + (new Vector3(direction.X, 0, direction.Y) * PanSpeed * distance * seconds);
        Position = new Vector3(Mathf.Clamp(moved.X, 0, map.Width), 0, Mathf.Clamp(moved.Z, 0, map.Height));

        distance = Mathf.Lerp(distance, targetDistance, 1 - Mathf.Exp(-ZoomSmoothing * seconds));
        PlaceCamera();
    }

    /// <summary>Which way the mouse at the edge of the screen pans: -1, 0 or 1 on each axis.</summary>
    private Vector2 EdgeDirection()
    {
        if (!mouseInWindow || !GetWindow().HasFocus())
        {
            return Vector2.Zero;
        }

        var viewport = GetViewport();
        var mouse = viewport.GetMousePosition();
        var size = viewport.GetVisibleRect().Size;

        return new Vector2(
            mouse.X <= EdgeMargin ? -1 : mouse.X >= size.X - EdgeMargin ? 1 : 0,
            mouse.Y <= EdgeMargin ? -1 : mouse.Y >= size.Y - EdgeMargin ? 1 : 0);
    }

    /// <summary>Puts the camera <see cref="distance"/> away from the ground point, looking down at it at the fixed angle.</summary>
    private void PlaceCamera()
    {
        var pitch = Mathf.DegToRad(PitchDegrees);

        camera.Position = new Vector3(0, distance * Mathf.Sin(pitch), distance * Mathf.Cos(pitch));
        camera.Rotation = new Vector3(-pitch, 0, 0);
    }

    private Vector3 MapCentre()
    {
        var map = MatchView.State.Map;

        return new Vector3(map.Width / 2f, 0, map.Height / 2f);
    }
}
