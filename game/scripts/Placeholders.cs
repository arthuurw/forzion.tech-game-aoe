using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Simple shapes standing in for the art until a whole match plays (spec #1): the terrain, the
/// resource sources, the buildings and the units. Colours tell the kinds and the owners apart.
/// </summary>
public sealed class Placeholders
{
    private const float BuildingHeight = 1.2f;
    private const float UnitRadius = 0.25f;
    private const float UnitHeight = 1.0f;

    private static readonly Color Grass = new(0.36f, 0.52f, 0.25f);
    private static readonly Color ForestGreen = new(0.1f, 0.3f, 0.12f);
    private static readonly Color WaterBlue = new(0.2f, 0.42f, 0.75f);
    private static readonly Color FoodRed = new(0.78f, 0.18f, 0.35f);
    private static readonly Color WoodBrown = new(0.55f, 0.35f, 0.15f);
    private static readonly Color GoldYellow = new(0.95f, 0.78f, 0.2f);
    private static readonly Color Neutral = new(0.6f, 0.6f, 0.6f);

    private static readonly Color[] PlayerColours =
    [
        new(0.2f, 0.45f, 0.95f),
        new(0.9f, 0.25f, 0.2f),
    ];

    private readonly Dictionary<Color, StandardMaterial3D> materials = [];

    private readonly Mesh foodMesh;
    private readonly Mesh woodMesh;
    private readonly Mesh goldMesh;
    private readonly Mesh unitMesh = new CapsuleMesh { Radius = UnitRadius, Height = UnitHeight };

    public Placeholders()
    {
        foodMesh = new SphereMesh { Radius = 0.35f, Height = 0.7f, Material = MaterialFor(FoodRed) };
        woodMesh = new CylinderMesh { TopRadius = 0.25f, BottomRadius = 0.3f, Height = 1.2f, Material = MaterialFor(WoodBrown) };
        goldMesh = new BoxMesh { Size = new Vector3(0.8f, 0.5f, 0.8f), Material = MaterialFor(GoldYellow) };
    }

    /// <summary>How high above the ground a unit's placeholder is placed, so it rests on the ground.</summary>
    public static float UnitStandingHeight => UnitHeight / 2;

    /// <summary>
    /// The shapes as the mouse picks them. A unit is picked a little beyond its drawn radius:
    /// at a distance the capsules are only a few pixels wide.
    /// </summary>
    /// <remarks>Buildings are the tallest of the shapes over Cells, as tall as the Wood trunk.</remarks>
    public static PickSizes PickSizes => new(
        UnitRadius: UnitRadius + 0.15,
        UnitHeight: UnitHeight,
        BuildingAndSourceHeight: BuildingHeight);

    /// <summary>The colour that marks what a Player owns.</summary>
    public static Color ColourOf(PlayerId player) =>
        player.Value >= 1 && player.Value <= PlayerColours.Length ? PlayerColours[player.Value - 1] : Neutral;

    /// <summary>
    /// The ground and the obstacles of the map: forests and water. Resource sources and
    /// buildings are entities with views of their own.
    /// </summary>
    public Node3D Terrain(MapState map)
    {
        var terrain = new Node3D { Name = "Terrain" };

        terrain.AddChild(new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = new Vector2(map.Width, map.Height), Material = MaterialFor(Grass) },
            Position = new Vector3(map.Width / 2f, 0, map.Height / 2f),
        });

        var forest = new CylinderMesh { TopRadius = 0, BottomRadius = 0.45f, Height = 1.4f, Material = MaterialFor(ForestGreen) };
        var water = new BoxMesh { Size = new Vector3(1, 0.05f, 1), Material = MaterialFor(WaterBlue) };

        terrain.AddChild(Scatter("Forest", forest, CellsOf(map, CellKind.Forest), height: 0.7f));
        terrain.AddChild(Scatter("Water", water, CellsOf(map, CellKind.Water), height: 0.01f));

        return terrain;
    }

    /// <summary>A shape for the source, placed on its Cell: a red sphere for Food, a brown trunk for Wood, a yellow block for Gold.</summary>
    public Node3D ResourceSource(ResourceSourceState source)
    {
        var (mesh, height) = source.Kind switch
        {
            ResourceKind.Food => (foodMesh, 0.35f),
            ResourceKind.Wood => (woodMesh, 0.6f),
            ResourceKind.Gold => (goldMesh, 0.25f),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source.Kind, "Unknown Resource."),
        };

        return new MeshInstance3D
        {
            Name = $"{source.Kind}Source{source.Id.Value}",
            Mesh = mesh,
            Position = WorldSpace.CentreOf(source.Cell, height),
        };
    }

    /// <summary>A box in the owner's colour covering the building's footprint, placed on it.</summary>
    public Node3D Building(BuildingState building)
    {
        // A small gap keeps neighbouring footprints apart on screen.
        var size = new Vector3(building.Width - 0.1f, BuildingHeight, building.Height - 0.1f);

        return new MeshInstance3D
        {
            Name = $"{building.Kind}{building.Id.Value}",
            Mesh = new BoxMesh { Size = size, Material = MaterialFor(ColourOf(building.Owner).Darkened(0.2f)) },
            Position = WorldSpace.CentreOf(building, BuildingHeight / 2),
        };
    }

    /// <summary>
    /// Raises a building's box as far as its construction has gone, from a low slab when the
    /// site is placed to its full height once complete.
    /// </summary>
    public static void ShowConstruction(Node3D view, BuildingState building)
    {
        const float LowestShare = 0.15f;

        var share = building.IsComplete
            ? 1
            : Math.Max(LowestShare, (float)Fractions.Of(building.BuildProgress, building.BuildTime));

        view.Scale = new Vector3(1, share, 1);
        view.Position = WorldSpace.CentreOf(building, BuildingHeight * share / 2);
    }

    /// <summary>A capsule in the owner's colour. It is not placed: units move, so the caller places it every frame.</summary>
    public Node3D Unit(UnitState unit) => new MeshInstance3D
    {
        Name = $"{unit.Kind}{unit.Id.Value}",
        Mesh = unitMesh,
        MaterialOverride = MaterialFor(ColourOf(unit.Owner)),
    };

    private static List<CellPosition> CellsOf(MapState map, CellKind kind)
    {
        var cells = new List<CellPosition>();

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var cell = new CellPosition(x, y);

                if (map[cell] == kind)
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    /// <summary>One copy of <paramref name="mesh"/> on each Cell, drawn in a single call.</summary>
    private static MultiMeshInstance3D Scatter(string name, Mesh mesh, List<CellPosition> cells, float height)
    {
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = cells.Count,
        };

        for (var index = 0; index < cells.Count; index++)
        {
            multiMesh.SetInstanceTransform(index, new Transform3D(Basis.Identity, WorldSpace.CentreOf(cells[index], height)));
        }

        return new MultiMeshInstance3D { Name = name, Multimesh = multiMesh };
    }

    private StandardMaterial3D MaterialFor(Color colour)
    {
        if (!materials.TryGetValue(colour, out var material))
        {
            material = new StandardMaterial3D { AlbedoColor = colour, Roughness = 0.8f };
            materials[colour] = material;
        }

        return material;
    }
}
