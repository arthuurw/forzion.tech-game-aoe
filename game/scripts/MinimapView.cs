using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The minimap in the bottom right corner of the screen: the terrain, the resource sources,
/// and the buildings and units of every Player in the Player's colour. Clicking it, or
/// dragging over it with the left button held, moves the camera to that place. Where each
/// thing goes in the box and which place a click means comes from <see cref="Minimap"/>; this
/// node only draws and forwards the mouse.
/// </summary>
public partial class MinimapView : Control
{
    /// <summary>Width of the minimap's frame, in pixels; it is as tall as the HUD's bottom panel.</summary>
    public const int FrameWidth = BoxWidth + (2 * Margin);

    // A 64 by 48 map fills the box at 4 pixels per Cell.
    private const int BoxWidth = 256;
    private const int BoxHeight = 192;
    private const int Margin = (Hud.PanelHeight - BoxHeight) / 2;

    private static readonly Color Ground = new(0.36f, 0.52f, 0.25f);
    private static readonly Color Forest = new(0.12f, 0.3f, 0.12f);
    private static readonly Color Water = new(0.2f, 0.42f, 0.75f);
    private static readonly Color Outline = new(0, 0, 0, 0.8f);

    private Minimap minimap = null!;
    private ImageTexture terrain = null!;

    /// <summary>The match the minimap shows.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The camera a click on the minimap moves.</summary>
    [Export]
    public RtsCamera CameraRig { get; set; } = null!;

    private static Vector2 BoxCorner => new(Margin, Margin);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        OffsetLeft = -FrameWidth;
        OffsetTop = -Hud.PanelHeight;
        OffsetRight = 0;
        OffsetBottom = 0;

        var map = MatchView.State.Map;
        minimap = new Minimap(map, BoxWidth, BoxHeight);
        terrain = TerrainOf(map);
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Hud.PanelColour);

        var mapCorner = BoxCorner + ToVector(minimap.Corner);
        var mapSize = new Vector2(terrain.GetWidth(), terrain.GetHeight()) * (float)minimap.Scale;
        DrawTextureRect(terrain, new Rect2(mapCorner, mapSize), tile: false);

        foreach (var mark in minimap.MarksOf(MatchView.State, MatchView.Driver.PositionOf))
        {
            var area = new Rect2(BoxCorner + ToVector(mark.Corner), (float)mark.Width, (float)mark.Height);

            switch (mark)
            {
                case ResourceSourceMark source:
                    DrawRect(area, ColourOf(source.Resource));
                    break;
                case PlayerMark owned:
                    // Outlined, so that a unit stands out on a resource source of a like colour.
                    DrawRect(area, Placeholders.ColourOf(owned.Owner));
                    DrawRect(area, Outline, filled: false, width: 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mark), mark, "Unknown mark.");
            }
        }

        DrawRect(new Rect2(mapCorner, mapSize), Outline, filled: false, width: 1);
    }

    public override void _GuiInput(InputEvent @event)
    {
        var dragging = @event is InputEventMouseMotion { ButtonMask: MouseButtonMask.Left };
        var clicked = @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true };

        if ((dragging || clicked) && @event is InputEventMouse mouse)
        {
            var inBox = mouse.Position - BoxCorner;
            CameraRig.CentreOn(minimap.ToMap(new ScreenPoint(inBox.X, inBox.Y)));
            AcceptEvent();
        }
    }

    /// <summary>The terrain, one pixel per Cell: forests and water on the ground. What stands on a Cell is drawn over it.</summary>
    private static ImageTexture TerrainOf(MapState map)
    {
        var image = Image.CreateEmpty(map.Width, map.Height, false, Image.Format.Rgb8);

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                image.SetPixel(x, y, ColourOf(map[new CellPosition(x, y)]));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static Color ColourOf(CellKind kind) => kind switch
    {
        CellKind.Forest => Forest,
        CellKind.Water => Water,

        // Resource sources and buildings come and go; the minimap draws them as marks.
        CellKind.Free or CellKind.ResourceSource or CellKind.Building => Ground,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kind of Cell."),
    };

    private static Color ColourOf(ResourceKind kind) => kind switch
    {
        ResourceKind.Food => new Color(0.9f, 0.2f, 0.3f),
        ResourceKind.Wood => new Color(0.5f, 0.32f, 0.14f),
        ResourceKind.Gold => new Color(1f, 0.85f, 0.2f),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Resource."),
    };

    private static Vector2 ToVector(ScreenPoint point) => new((float)point.X, (float)point.Y);
}
