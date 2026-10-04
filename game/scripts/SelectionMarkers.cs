using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Shows what is selected: a flat ring on the ground around each selected unit or building,
/// following the unit as it walks.
/// </summary>
public partial class SelectionMarkers : Node3D
{
    private const float RingHeight = 0.03f;
    private const float UnitRingRadius = 0.42f;

    private static readonly Color RingColour = new(1, 1, 1);

    private readonly Dictionary<EntityId, MeshInstance3D> rings = [];
    private readonly HashSet<EntityId> shown = [];

    private TorusMesh ringMesh = null!;

    /// <summary>The match whose entities are marked.</summary>
    [Export]
    public MatchView MatchView { get; set; } = null!;

    /// <summary>The input whose selection is marked.</summary>
    [Export]
    public SelectionInput SelectionInput { get; set; } = null!;

    public override void _Ready()
    {
        // A ring of radius 1, scaled to each entity's size.
        ringMesh = new TorusMesh
        {
            InnerRadius = 0.9f,
            OuterRadius = 1,
            Rings = 48,
            Material = new StandardMaterial3D
            {
                AlbedoColor = RingColour,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
    }

    public override void _Process(double delta)
    {
        var state = MatchView.State;
        shown.Clear();

        foreach (var id in SelectionInput.Control.Selected)
        {
            var placed = PlaceRing(id, state);

            if (placed)
            {
                shown.Add(id);
            }
        }

        foreach (var id in rings.Keys.Where(id => !shown.Contains(id)).ToList())
        {
            rings[id].QueueFree();
            rings.Remove(id);
        }
    }

    /// <summary>Puts the entity's ring around it; false when the entity is no longer in the match.</summary>
    private bool PlaceRing(EntityId id, MatchState state)
    {
        var unit = state.Units.FirstOrDefault(unit => unit.Id == id);
        var building = unit is null ? state.Buildings.FirstOrDefault(building => building.Id == id) : null;

        if (unit is null && building is null)
        {
            return false;
        }

        var ring = RingOf(id);

        if (unit is not null)
        {
            ring.Position = WorldSpace.ToWorld(MatchView.Driver.PositionOf(unit), RingHeight);
            ring.Scale = new Vector3(UnitRingRadius, 1, UnitRingRadius);
        }
        else
        {
            ring.Position = WorldSpace.CentreOf(building!, RingHeight);

            // An ellipse just outside the footprint.
            ring.Scale = new Vector3((building!.Width / 2f) + 0.3f, 1, (building.Height / 2f) + 0.3f);
        }

        return true;
    }

    private MeshInstance3D RingOf(EntityId id)
    {
        if (!rings.TryGetValue(id, out var ring))
        {
            ring = new MeshInstance3D
            {
                Name = $"SelectionRing{id.Value}",
                Mesh = ringMesh,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            rings[id] = ring;
            AddChild(ring);
        }

        return ring;
    }
}
