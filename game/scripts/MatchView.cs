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
    private static readonly FactionId Portuguese = new(1);

    /// <summary>Seed of the match. The same seed always generates the same map.</summary>
    [Export]
    public ulong Seed { get; set; } = 1;

    [Export]
    public int MapWidth { get; set; } = 64;

    [Export]
    public int MapHeight { get; set; } = 48;

    public MatchDriver Driver { get; private set; } = null!;

    public Match Match => Driver.Match;

    public override void _Ready()
    {
        var config = new MatchConfig(
            Seed,
            new MapConfig(MapWidth, MapHeight),
            [new PlayerConfig(Portuguese), new PlayerConfig(Portuguese)]);

        Driver = new MatchDriver(Match.Create(config), new TickClock(Match.TicksPerSecond));

        var state = Match.State;
        GD.Print(
            $"Match created: seed {Seed}, map {state.Map.Width}x{state.Map.Height}, {state.Players.Count} Players, " +
            $"{state.ResourceSources.Count} resource sources, {state.Buildings.Count} buildings, {state.Units.Count} units.");
    }
}
