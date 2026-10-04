using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Shows what is selected: a flat ring on the ground around each selected unit or building,
/// following the unit as it walks, and a flag on the rally point of a selected building.
/// </summary>
public partial class SelectionMarkers : Node3D
{
    private const float RingHeight = 0.03f;
    private const float UnitRingRadius = 0.42f;

    private static readonly Color RingColour = new(1, 1, 1);

    private readonly Dictionary<EntityId, MeshInstance3D> rings = [];
    private readonly HashSet<EntityId> shown = [];

    private TorusMesh ringMesh = null!;
    private Node3D rallyFlag = null!;

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

        rallyFlag = RallyFlag();
        AddChild(rallyFlag);
    }

    public override void _Process(double delta)
    {
        var state = MatchView.State;
        shown.Clear();

        foreach (var id in SelectionInput.PlayerControl.Selected)
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

        var rallyPoint = state.Buildings
            .Where(building => shown.Contains(building.Id))
            .Select(building => building.RallyPoint)
            .FirstOrDefault(cell => cell is not null);

        rallyFlag.Visible = rallyPoint is not null;

        if (rallyPoint is { } cell)
        {
            rallyFlag.Position = WorldSpace.CentreOf(cell);
        }
    }

    /// <summary>A pole with a pennant in the human Player's colour, standing on the ground.</summary>
    private static Node3D RallyFlag()
    {
        var flag = new Node3D { Name = "RallyFlag", Visible = false };
        var colour = Palette.ColourOf(MatchView.HumanPlayer);

        flag.AddChild(new MeshInstance3D
        {
            Name = "Pole",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.03f,
                BottomRadius = 0.03f,
                Height = 1.4f,
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.9f) },
            },
            Position = new Vector3(0, 0.7f, 0),
        });
        flag.AddChild(new MeshInstance3D
        {
            Name = "Pennant",
            Mesh = new BoxMesh
            {
                Size = new Vector3(0.45f, 0.3f, 0.03f),
                Material = new StandardMaterial3D { AlbedoColor = colour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            },
            Position = new Vector3(0.24f, 1.22f, 0),
        });

        return flag;
    }

    /// <summary>Puts the entity's ring around it; false when the entity is no longer in the match.</summary>
    private bool PlaceRing(EntityId id, MatchState state)
    {
        var (unit, building) = state.FindUnitOrBuilding(id);

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
