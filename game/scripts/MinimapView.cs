using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// The minimap in the bottom right corner of the screen: the terrain, the resource sources,
/// and the buildings and units of every Player in the Player's colour. Clicking it moves the
/// camera to that place, and dragging on from the click takes the camera along. Where each
/// thing goes in the box and which place a click means comes from <see cref="Minimap"/>; this
/// node only draws and forwards the mouse.
/// </summary>
/// <remarks>
/// The mouse passes through to the map unless the left button was pressed on the minimap, so
/// a selection box dragged from the map over the minimap still grows and ends there.
/// </remarks>
public partial class MinimapView : Control
{
    private static readonly Color Outline = new(0, 0, 0, 0.8f);

    private Minimap minimap = null!;
    private ImageTexture terrain = null!;

    // Whether the left button went down on the minimap and is still held: only then does the
    // mouse move the camera.
    private bool movingCamera;

    /// <summary>The match the minimap shows.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The camera a click on the minimap moves.</summary>
    [Export]
    public RtsCamera CameraRig { get; set; } = null!;

    private static Vector2 BoxCorner => new(HudLayout.MinimapMargin, HudLayout.MinimapMargin);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        OffsetLeft = -HudLayout.MinimapWidth;
        OffsetTop = -HudLayout.BottomHeight;
        OffsetRight = 0;
        OffsetBottom = 0;

        var map = MatchView.State.Map;
        minimap = new Minimap(map, HudLayout.MinimapBoxWidth, HudLayout.MinimapBoxHeight);
        terrain = TerrainOf(map);
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Palette.Panel);

        var mapCorner = BoxCorner + ToVector(minimap.Corner);
        var mapSize = new Vector2(terrain.GetWidth(), terrain.GetHeight()) * (float)minimap.Scale;
        DrawTextureRect(terrain, new Rect2(mapCorner, mapSize), tile: false);

        foreach (var mark in minimap.MarksOf(MatchView.State, MatchView.Driver.PositionOf))
        {
            var area = new Rect2(BoxCorner + ToVector(mark.Corner), (float)mark.Width, (float)mark.Height);

            switch (mark)
            {
                case ResourceSourceMark source:
                    DrawRect(area, Palette.ColourOf(source.Resource));
                    break;
                case PlayerMark owned:
                    // Outlined, so that a unit stands out on a resource source of a like colour.
                    DrawRect(area, Palette.ColourOf(owned.Owner));
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
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                movingCamera = true;
                CentreCameraOn(click.Position);
                break;
            case InputEventMouseMotion motion when movingCamera:
                CentreCameraOn(motion.Position);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when movingCamera:
                movingCamera = false;
                AcceptEvent();
                break;
        }
    }

    private void CentreCameraOn(Vector2 position)
    {
        var inBox = position - BoxCorner;
        CameraRig.CentreOn(minimap.ToMap(new ScreenPoint(inBox.X, inBox.Y)));
        AcceptEvent();
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
        CellKind.Forest => Palette.Forest,
        CellKind.Water => Palette.Water,

        // Resource sources and buildings come and go; the minimap draws them as marks.
        CellKind.Free or CellKind.ResourceSource or CellKind.Building => Palette.Ground,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kind of Cell."),
    };

    private static Vector2 ToVector(ScreenPoint point) => new((float)point.X, (float)point.Y);
}
