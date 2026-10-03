using Forzion.Presentation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Shows where the building chosen in the HUD would go: a see-through box over its footprint
/// under the mouse, green where the match would place it and red where it would not. Whether
/// the spot is valid is the match's to say, through <see cref="PlayerControl.PlacementAt"/>.
/// </summary>
public partial class PlacementPreview : Node3D
{
    private const float Height = 1.2f;

    private static readonly Color Valid = new(0.3f, 0.95f, 0.4f, 0.45f);
    private static readonly Color Invalid = new(0.95f, 0.25f, 0.2f, 0.45f);

    private MeshInstance3D box = null!;
    private BoxMesh mesh = null!;
    private StandardMaterial3D material = null!;
    private ScreenPoint? pointer;

    /// <summary>The input whose chosen building is previewed.</summary>
    [Export]
    public SelectionInput SelectionInput { get; set; } = null!;

    public override void _Ready()
    {
        material = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = Valid,
        };
        mesh = new BoxMesh { Material = material };
        box = new MeshInstance3D
        {
            Name = "Footprint",
            Mesh = mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        AddChild(box);
    }

    // Follows the mouse events rather than asking where the pointer is, so the preview sits
    // where the next click lands, whatever sends the events.
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouse mouse)
        {
            pointer = new ScreenPoint(mouse.Position.X, mouse.Position.Y);
        }
    }

    public override void _Process(double delta)
    {
        var placement = pointer is { } at ? SelectionInput.Control.PlacementAt(at) : null;

        box.Visible = placement is not null;

        if (placement is null)
        {
            return;
        }

        mesh.Size = new Vector3(placement.Size, Height, placement.Size);
        box.Position = new Vector3(placement.Origin.X + (placement.Size / 2f), Height / 2, placement.Origin.Y + (placement.Size / 2f));
        material.AlbedoColor = placement.IsValid ? Valid : Invalid;
    }
}
