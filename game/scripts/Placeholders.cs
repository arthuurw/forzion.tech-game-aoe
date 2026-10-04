using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Simple shapes standing in for the art until a whole match plays (spec #1): the terrain, the
/// resource sources, the buildings and the units. Each kind has a silhouette of its own, and
/// what a Player owns wears the Player's colour.
/// </summary>
/// <remarks>
/// Every view is a node at ground level whose parts are built from Godot's primitive meshes,
/// laid out in world units (one per Cell) with +Z towards the camera.
/// </remarks>
public sealed class Placeholders
{
    /// <summary>How tall the tallest unit stands: the heavy soldier, to the top of the rider's helmet.</summary>
    public const float TallestUnit = 1.45f;

    /// <summary>How tall the tallest building stands: the Town Center, to the ridge of its tower.</summary>
    public const float TallestBuilding = 2.25f;

    private const float TallestSource = 0.6f;

    /// <summary>
    /// How far from a unit's position the mouse still picks it: past the body, which is only a
    /// few pixels wide at a distance, and over most of the heavy soldier's horse.
    /// </summary>
    private const float UnitPickRadius = 0.5f;

    /// <summary>How see-through a construction site's unfinished building is.</summary>
    private const float SiteTransparency = 0.45f;

    private static readonly Color Grass = new(0.36f, 0.52f, 0.25f);
    private static readonly Color ForestGreen = new(0.1f, 0.3f, 0.12f);
    private static readonly Color WaterBlue = new(0.2f, 0.42f, 0.75f);
    private static readonly Color BushGreen = new(0.2f, 0.4f, 0.1f);
    private static readonly Color BerryRed = new(0.85f, 0.1f, 0.2f);
    private static readonly Color LogBrown = new(0.55f, 0.35f, 0.15f);
    private static readonly Color RockGrey = new(0.5f, 0.5f, 0.52f);
    private static readonly Color GoldYellow = new(0.98f, 0.8f, 0.15f);
    private static readonly Color WallWhite = new(0.86f, 0.82f, 0.72f);
    private static readonly Color DoorBrown = new(0.25f, 0.17f, 0.1f);
    private static readonly Color TimberBrown = new(0.45f, 0.3f, 0.15f);
    private static readonly Color CrateBrown = new(0.62f, 0.46f, 0.26f);
    private static readonly Color BareEarth = new(0.5f, 0.42f, 0.3f);
    private static readonly Color Skin = new(0.85f, 0.66f, 0.5f);
    private static readonly Color Straw = new(0.88f, 0.78f, 0.5f);
    private static readonly Color Steel = new(0.74f, 0.76f, 0.8f);
    private static readonly Color GunWood = new(0.28f, 0.18f, 0.1f);
    private static readonly Color HorseBrown = new(0.42f, 0.27f, 0.15f);
    private static readonly Color Neutral = new(0.6f, 0.6f, 0.6f);

    // Violet and orange: neither is the blue of Water, the greens of the ground, Forests and
    // Food, nor the brown of Wood and the yellow of Gold.
    private static readonly Color[] PlayerColours =
    [
        new(0.6f, 0.3f, 0.9f),
        new(0.95f, 0.5f, 0.1f),
    ];

    private readonly Dictionary<Color, StandardMaterial3D> materials = [];

    /// <summary>
    /// The shapes as the mouse picks them: each as tall as the tallest of its kind, so that
    /// pointing anywhere at one picks it.
    /// </summary>
    public static PickSizes PickSizes => new(
        UnitRadius: UnitPickRadius,
        UnitHeight: TallestUnit,
        BuildingHeight: TallestBuilding,
        SourceHeight: TallestSource);

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

    /// <summary>
    /// A low shape for the source, placed on its Cell: a bush with red berries for Food, a
    /// pile of logs for Wood, a grey rock studded with nuggets for Gold.
    /// </summary>
    public Node3D ResourceSource(ResourceSourceState source)
    {
        var view = new Node3D
        {
            Name = $"{source.Kind}Source{source.Id.Value}",
            Position = WorldSpace.CentreOf(source.Cell),
        };

        switch (source.Kind)
        {
            case ResourceKind.Food:
                AddBerryBush(view);
                break;
            case ResourceKind.Wood:
                AddLogPile(view);
                break;
            case ResourceKind.Gold:
                AddGoldRock(view);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source.Kind, "Unknown Resource.");
        }

        return view;
    }

    /// <summary>
    /// The building's silhouette on its footprint, roofed in the owner's colour: the Town
    /// Center's tower, the House's pointed roof, the Storehouse's open shed with its crates,
    /// the Barracks' long battlemented hall. A construction site keeps the silhouette, see
    /// <see cref="ShowConstruction"/>.
    /// </summary>
    public Node3D Building(BuildingState building)
    {
        var view = new Node3D
        {
            Name = $"{building.Kind}{building.Id.Value}",
            Position = WorldSpace.CentreOf(building),
        };
        var body = new Node3D { Name = "Body" };
        var roof = ColourOf(building.Owner);

        // A small gap keeps neighbouring footprints apart on screen.
        var width = building.Width - 0.1f;
        var depth = building.Height - 0.1f;

        var height = building.Kind switch
        {
            BuildingKind.TownCenter => AddTownCenter(body, width, depth, roof),
            BuildingKind.House => AddHouse(body, width, depth, roof),
            BuildingKind.Storehouse => AddStorehouse(body, width, depth, roof),
            BuildingKind.Barracks => AddBarracks(body, width, depth, roof),
            _ => throw new ArgumentOutOfRangeException(nameof(building), building.Kind, "Unknown building."),
        };

        view.AddChild(body);
        view.AddChild(Scaffold(width, depth, height));

        return view;
    }

    /// <summary>
    /// Shows how far a building's construction has gone. A construction site stands in
    /// scaffolding on bare earth, its silhouette see-through and raised as far as it is built,
    /// from a low stub when placed to full height; a complete building stands whole and solid.
    /// </summary>
    public static void ShowConstruction(Node3D view, BuildingState building)
    {
        const float LowestShare = 0.15f;

        var share = building.IsComplete
            ? 1
            : Math.Max(LowestShare, (float)Fractions.Of(building.BuildProgress, building.BuildTime));
        var body = view.GetNode<Node3D>("Body");

        body.Scale = new Vector3(1, share, 1);
        view.GetNode<Node3D>("Scaffold").Visible = !building.IsComplete;

        foreach (var part in body.GetChildren().OfType<GeometryInstance3D>())
        {
            part.Transparency = building.IsComplete ? 0 : SiteTransparency;
        }
    }

    /// <summary>
    /// A figure dressed in the owner's colour, told apart by what it carries: the Villager's
    /// straw hat, the melee soldier's round shield and sword, the ranged soldier's long gun,
    /// the heavy soldier's horse and lance. It is not placed: units move, so the caller places
    /// it every frame, on the ground.
    /// </summary>
    public Node3D Unit(UnitState unit)
    {
        var view = new Node3D { Name = $"{unit.Kind}{unit.Id.Value}" };
        var colour = ColourOf(unit.Owner);

        switch (unit.Kind)
        {
            case UnitKind.Villager:
                AddVillager(view, colour);
                break;
            case UnitKind.MeleeSoldier:
                AddMeleeSoldier(view, colour);
                break;
            case UnitKind.RangedSoldier:
                AddRangedSoldier(view, colour);
                break;
            case UnitKind.HeavySoldier:
                AddHeavySoldier(view, colour);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(unit), unit.Kind, "Unknown unit.");
        }

        return view;
    }

    private void AddBerryBush(Node3D view)
    {
        view.AddChild(Part("Bush", Ball(0.3f, 0.4f), BushGreen, new Vector3(0, 0.2f, 0)));

        Vector3[] berries =
        [
            new(0, 0.4f, 0.02f), new(0.17f, 0.32f, 0.12f), new(-0.17f, 0.32f, 0.1f), new(0.04f, 0.3f, 0.24f),
            new(-0.08f, 0.33f, -0.17f), new(0.2f, 0.26f, -0.12f), new(-0.22f, 0.22f, -0.04f),
        ];

        foreach (var at in berries)
        {
            view.AddChild(Part("Berry", Ball(0.065f), BerryRed, at));
        }
    }

    private void AddLogPile(Node3D view)
    {
        // Three logs, two on them, one on top, all lying along X.
        (float Y, float Z)[] logs = [(0.1f, -0.21f), (0.1f, 0), (0.1f, 0.21f), (0.27f, -0.105f), (0.27f, 0.105f), (0.44f, 0)];

        foreach (var (y, z) in logs)
        {
            view.AddChild(Part("Log", Cylinder(0.1f, 0.8f), LogBrown, new Vector3(0, y, z), new Vector3(0, 0, 90)));
        }
    }

    private void AddGoldRock(Node3D view)
    {
        view.AddChild(Part("Rock", Box(0.55f, 0.3f, 0.5f), RockGrey, new Vector3(0, 0.15f, 0), new Vector3(0, 20, 0)));

        Vector3[] nuggets =
        [
            new(0.12f, 0.33f, 0.05f), new(-0.12f, 0.32f, -0.08f), new(0.02f, 0.35f, -0.16f),
            new(0.22f, 0.2f, 0.2f), new(-0.2f, 0.18f, 0.18f),
        ];

        foreach (var at in nuggets)
        {
            view.AddChild(Part("Nugget", Box(0.17f, 0.17f, 0.17f), GoldYellow, at, new Vector3(30, 45, 0)));
        }
    }

    /// <summary>Walls under broad eaves, and a tower with a pitched roof rising from the middle. Returns how tall it stands.</summary>
    private float AddTownCenter(Node3D body, float width, float depth, Color roof)
    {
        body.AddChild(Part("Walls", Box(width * 0.92f, 0.8f, depth * 0.92f), WallWhite, new Vector3(0, 0.4f, 0)));
        body.AddChild(Part("Eaves", Box(width, 0.12f, depth), roof, new Vector3(0, 0.86f, 0)));
        body.AddChild(Part("Tower", Box(1, 0.75f, 1), WallWhite, new Vector3(0, 1.295f, 0)));
        body.AddChild(Part("TowerRoof", Gable(1.25f, 0.55f, 1.25f), roof, new Vector3(0, 1.945f, 0)));
        body.AddChild(Part("Door", Box(0.5f, 0.5f, 0.04f), DoorBrown, new Vector3(0, 0.25f, (depth * 0.46f) + 0.02f)));

        return 2.22f;
    }

    /// <summary>Small walls under a pitched roof whose ridge runs across the screen. Returns how tall it stands.</summary>
    private float AddHouse(Node3D body, float width, float depth, Color roof)
    {
        body.AddChild(Part("Walls", Box(width * 0.8f, 0.65f, depth * 0.8f), WallWhite, new Vector3(0, 0.325f, 0)));
        body.AddChild(Part("Roof", Gable(width * 0.95f, 0.6f, depth * 0.95f), roof, new Vector3(0, 0.95f, 0)));
        body.AddChild(Part("Door", Box(0.3f, 0.4f, 0.04f), DoorBrown, new Vector3(0, 0.2f, (depth * 0.4f) + 0.02f)));

        return 1.25f;
    }

    /// <summary>
    /// An open yard: a lean-to roof on posts over its back half, crates, barrels and sacks in
    /// the open before it. Returns how tall it stands.
    /// </summary>
    private float AddStorehouse(Node3D body, float width, float depth, Color roof)
    {
        body.AddChild(Part("Floor", Box(width * 0.95f, 0.06f, depth * 0.95f), TimberBrown, new Vector3(0, 0.03f, 0)));

        foreach (var x in new[] { -1, 1 })
        {
            body.AddChild(Part("Post", Cylinder(0.05f, 0.9f), TimberBrown, new Vector3(x * width * 0.42f, 0.45f, -depth * 0.42f)));
            body.AddChild(Part("Post", Cylinder(0.05f, 0.6f), TimberBrown, new Vector3(x * width * 0.42f, 0.3f, -depth * 0.02f)));
        }

        body.AddChild(Part("Roof", Box(width * 0.95f, 0.08f, depth * 0.5f), roof, new Vector3(0, 0.78f, -depth * 0.22f), new Vector3(-17, 0, 0)));

        body.AddChild(Part("Crate", Box(0.36f, 0.36f, 0.36f), CrateBrown, new Vector3(-0.45f, 0.24f, 0.4f), new Vector3(0, 15, 0)));
        body.AddChild(Part("Crate", Box(0.3f, 0.3f, 0.3f), CrateBrown, new Vector3(-0.45f, 0.57f, 0.4f), new Vector3(0, -20, 0)));
        body.AddChild(Part("Crate", Box(0.34f, 0.34f, 0.34f), CrateBrown, new Vector3(0.05f, 0.23f, 0.55f), new Vector3(0, -10, 0)));
        body.AddChild(Part("Barrel", Cylinder(0.15f, 0.42f), TimberBrown, new Vector3(0.5f, 0.27f, 0.35f)));
        body.AddChild(Part("Barrel", Cylinder(0.15f, 0.42f), TimberBrown, new Vector3(0.5f, 0.27f, 0.7f)));
        body.AddChild(Part("Sack", Ball(0.16f, 0.24f), Straw, new Vector3(-0.05f, 0.15f, 0.15f)));
        body.AddChild(Part("Sack", Ball(0.16f, 0.24f), Straw, new Vector3(0.25f, 0.15f, 0.05f)));

        return 0.92f;
    }

    /// <summary>A long hall with a battlemented flat roof. Returns how tall it stands.</summary>
    private float AddBarracks(Node3D body, float width, float depth, Color roof)
    {
        body.AddChild(Part("Walls", Box(width * 0.9f, 0.9f, depth * 0.7f), WallWhite, new Vector3(0, 0.45f, 0)));
        body.AddChild(Part("Roof", Box(width * 0.94f, 0.1f, depth * 0.74f), roof, new Vector3(0, 0.95f, 0)));

        for (var index = 0; index < 5; index++)
        {
            var x = width * (-0.36f + (0.18f * index));

            foreach (var z in new[] { -1, 1 })
            {
                body.AddChild(Part("Merlon", Box(0.25f, 0.22f, 0.12f), roof, new Vector3(x, 1.11f, z * depth * 0.34f)));
            }
        }

        body.AddChild(Part("Door", Box(0.6f, 0.6f, 0.04f), DoorBrown, new Vector3(0, 0.3f, (depth * 0.35f) + 0.02f)));

        return 1.22f;
    }

    /// <summary>Bare earth under the footprint, a timber post at each corner and rails between them, as tall as the building.</summary>
    private Node3D Scaffold(float width, float depth, float height)
    {
        var scaffold = new Node3D { Name = "Scaffold" };

        scaffold.AddChild(Part("Earth", Box(width, 0.04f, depth), BareEarth, new Vector3(0, 0.02f, 0)));

        foreach (var x in new[] { -1, 1 })
        {
            foreach (var z in new[] { -1, 1 })
            {
                scaffold.AddChild(Part("Post", Cylinder(0.04f, height), TimberBrown, new Vector3(x * width / 2, height / 2, z * depth / 2)));
            }
        }

        foreach (var y in new[] { height * 0.45f, height * 0.9f })
        {
            foreach (var side in new[] { -1, 1 })
            {
                scaffold.AddChild(Part("Rail", Box(width, 0.05f, 0.05f), TimberBrown, new Vector3(0, y, side * depth / 2)));
                scaffold.AddChild(Part("Rail", Box(0.05f, 0.05f, depth), TimberBrown, new Vector3(side * width / 2, y, 0)));
            }
        }

        return scaffold;
    }

    /// <summary>A body in the given colour and a head on it, 0.96 tall.</summary>
    private void AddFigure(Node3D view, Color colour)
    {
        view.AddChild(Part("Body", Capsule(0.17f, 0.72f), colour, new Vector3(0, 0.36f, 0)));
        view.AddChild(Part("Head", Ball(0.12f), Skin, new Vector3(0, 0.84f, 0)));
    }

    private void AddVillager(Node3D view, Color colour)
    {
        AddFigure(view, colour);
        view.AddChild(Part("Hat", Cone(0.02f, 0.18f, 0.16f), Straw, new Vector3(0, 0.98f, 0)));
    }

    private void AddMeleeSoldier(Node3D view, Color colour)
    {
        AddFigure(view, colour);
        view.AddChild(Part("Helmet", Ball(0.14f, 0.16f), Steel, new Vector3(0, 0.9f, 0)));
        view.AddChild(Part("Shield", Cylinder(0.25f, 0.05f), Steel, new Vector3(-0.1f, 0.45f, 0.2f), new Vector3(90, 0, 0)));
        view.AddChild(Part("Sword", Box(0.04f, 0.45f, 0.03f), Steel, new Vector3(0.24f, 0.5f, 0.05f), new Vector3(0, 0, -15)));
    }

    private void AddRangedSoldier(Node3D view, Color colour)
    {
        AddFigure(view, colour);
        view.AddChild(Part("Helmet", Ball(0.13f, 0.14f), Steel, new Vector3(0, 0.92f, 0)));

        // Held level and pointing ahead, the gun reaches well beyond the body on both sides.
        view.AddChild(Part("Gun", Cylinder(0.035f, 1.1f), GunWood, new Vector3(0.2f, 0.62f, 0.14f), new Vector3(0, 0, -80)));
    }

    private void AddHeavySoldier(Node3D view, Color colour)
    {
        view.AddChild(Part("Horse", Capsule(0.17f, 0.85f), HorseBrown, new Vector3(0, 0.62f, 0), new Vector3(0, 0, 90)));

        foreach (var x in new[] { -0.28f, 0.28f })
        {
            foreach (var z in new[] { -0.09f, 0.09f })
            {
                view.AddChild(Part("Leg", Cylinder(0.045f, 0.5f), HorseBrown, new Vector3(x, 0.25f, z)));
            }
        }

        view.AddChild(Part("Neck", Capsule(0.08f, 0.5f), HorseBrown, new Vector3(0.4f, 0.86f, 0), new Vector3(0, 0, -35)));
        view.AddChild(Part("HorseHead", Box(0.3f, 0.13f, 0.14f), HorseBrown, new Vector3(0.6f, 1.02f, 0), new Vector3(0, 0, -30)));
        view.AddChild(Part("Tail", Cylinder(0.035f, 0.4f), DoorBrown, new Vector3(-0.47f, 0.55f, 0), new Vector3(0, 0, -25)));
        view.AddChild(Part("SaddleCloth", Box(0.4f, 0.1f, 0.38f), colour, new Vector3(-0.02f, 0.76f, 0)));
        view.AddChild(Part("Rider", Capsule(0.14f, 0.55f), colour, new Vector3(-0.02f, 1.02f, 0)));
        view.AddChild(Part("Head", Ball(0.1f), Skin, new Vector3(-0.02f, 1.33f, 0)));
        view.AddChild(Part("Helmet", Ball(0.11f, 0.12f), Steel, new Vector3(-0.02f, 1.39f, 0)));
        view.AddChild(Part("Lance", Cylinder(0.02f, 1.1f), TimberBrown, new Vector3(0.25f, 1.05f, 0.18f), new Vector3(0, 0, -60)));
    }

    private MeshInstance3D Part(string name, Mesh mesh, Color colour, Vector3 position, Vector3 rotationDegrees = default) => new()
    {
        Name = name,
        Mesh = mesh,
        MaterialOverride = MaterialFor(colour),
        Position = position,
        RotationDegrees = rotationDegrees,
    };

    private static BoxMesh Box(float width, float height, float depth) => new() { Size = new Vector3(width, height, depth) };

    private static CylinderMesh Cylinder(float radius, float height) => new() { TopRadius = radius, BottomRadius = radius, Height = height };

    private static CylinderMesh Cone(float topRadius, float bottomRadius, float height) =>
        new() { TopRadius = topRadius, BottomRadius = bottomRadius, Height = height };

    /// <summary>
    /// A pitched roof <paramref name="width"/> along X and <paramref name="depth"/> along Z
    /// once turned 90 degrees about Y, which puts its ridge along X and a slope towards the camera.
    /// </summary>
    private static PrismMesh Gable(float width, float height, float depth) => new() { Size = new Vector3(depth, height, width) };

    /// <summary>A sphere, or a squashed one when <paramref name="height"/> is less than its diameter.</summary>
    private static SphereMesh Ball(float radius, float? height = null) => new() { Radius = radius, Height = height ?? radius * 2 };

    private static CapsuleMesh Capsule(float radius, float height) => new() { Radius = radius, Height = height };

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
