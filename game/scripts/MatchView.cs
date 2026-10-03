using Forzion.Presentation;
using Forzion.Simulation;
using Godot;

namespace Forzion.Game;

/// <summary>
/// Owns the running match and shows it. It holds no game rule (ADR 0001): it only creates the
/// match, drives its ticks and reads its state.
/// </summary>
public partial class MatchView : Node3D
{
    /// <summary>The Player controlled by the person at the screen.</summary>
    public static readonly PlayerId HumanPlayer = new(1);

    private static readonly FactionId Portuguese = new(1);

    private readonly Placeholders placeholders = new();
    private readonly Dictionary<EntityId, Node3D> sourceViews = [];
    private readonly Dictionary<EntityId, Node3D> buildingViews = [];
    private readonly Dictionary<EntityId, Node3D> unitViews = [];
    private readonly HashSet<EntityId> seen = [];

    /// <summary>Seed of the match. The same seed always generates the same map.</summary>
    [Export]
    public ulong Seed { get; set; } = 1;

    /// <summary>Width of the map in Cells.</summary>
    [Export]
    public int MapWidth { get; set; } = 64;

    /// <summary>Height of the map in Cells.</summary>
    [Export]
    public int MapHeight { get; set; } = 48;

    /// <summary>Drives the match's ticks. Set in <see cref="_Ready"/>, so nodes that read it must come after this one in the scene.</summary>
    public MatchDriver Driver { get; private set; } = null!;

    /// <summary>The running match: read its state and enqueue commands on it; this node ticks it.</summary>
    public Match Match => Driver.Match;

    public override void _Ready()
    {
        var config = new MatchConfig(
            Seed,
            new MapConfig(MapWidth, MapHeight),
            [new PlayerConfig(Portuguese), new PlayerConfig(Portuguese)]);

        Driver = new MatchDriver(Match.Create(config), new TickClock(Match.TicksPerSecond));

        var state = Match.State;

        AddChild(placeholders.Terrain(state.Map));
        SyncViews();

        GD.Print(
            $"Match created: seed {Seed}, map {state.Map.Width}x{state.Map.Height}, {state.Players.Count} Players, " +
            $"{sourceViews.Count} resource sources, {buildingViews.Count} buildings, {unitViews.Count} units shown.");
    }

    /// <summary>
    /// Raised once per frame that ran ticks, with the events of those ticks in the order they
    /// happened, for the nodes that report them on screen.
    /// </summary>
    public event Action<IReadOnlyList<MatchEvent>>? EventsHappened;

    /// <summary>
    /// Runs every frame: lets the frame's time pass in the match, which runs the ticks that
    /// became due (20 per second whatever the frame rate), then redraws.
    /// </summary>
    public override void _Process(double delta)
    {
        var events = Driver.Advance(delta);
        SyncViews();

        if (events.Count > 0)
        {
            EventsHappened?.Invoke(events);
        }
    }

    /// <summary>
    /// Makes the views match the state: a view for each entity that appeared, none for each
    /// that is gone, every construction site as high as it is built, and every unit drawn where
    /// it is in this frame.
    /// </summary>
    private void SyncViews()
    {
        var state = Match.State;

        Sync(state.ResourceSources, sourceViews, source => source.Id, placeholders.ResourceSource);
        Sync(state.Buildings, buildingViews, building => building.Id, placeholders.Building);
        Sync(state.Units, unitViews, unit => unit.Id, placeholders.Unit);

        foreach (var building in state.Buildings)
        {
            Placeholders.ShowConstruction(buildingViews[building.Id], building);
        }

        foreach (var unit in state.Units)
        {
            unitViews[unit.Id].Position = WorldSpace.ToWorld(Driver.PositionOf(unit), Placeholders.UnitStandingHeight);
        }
    }

    private void Sync<TEntity>(
        IReadOnlyList<TEntity> entities,
        Dictionary<EntityId, Node3D> views,
        Func<TEntity, EntityId> idOf,
        Func<TEntity, Node3D> createView)
    {
        seen.Clear();

        foreach (var entity in entities)
        {
            var id = idOf(entity);
            seen.Add(id);

            if (!views.ContainsKey(id))
            {
                var view = createView(entity);
                views[id] = view;
                AddChild(view);
            }
        }

        foreach (var id in views.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            views[id].QueueFree();
            views.Remove(id);
        }
    }
}
