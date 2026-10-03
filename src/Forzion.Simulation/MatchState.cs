namespace Forzion.Simulation;

/// <summary>
/// The state of a match: plain data identified by ID, read-only from outside the simulation.
/// Only the simulation's own tick changes it. Collections are ordered by ascending ID.
/// </summary>
public sealed class MatchState
{
    private readonly List<PlayerState> players;

    internal MatchState(MatchConfig config)
    {
        Random = new MatchRandom(config.Seed);
        Map = new MapState(config.Map.Width, config.Map.Height);
        players = config.Players
            .Select((player, index) => new PlayerState(new PlayerId(index + 1), player.Faction))
            .ToList();
    }

    /// <summary>Number of ticks simulated so far.</summary>
    public int Tick { get; internal set; }

    public MapState Map { get; }

    internal MatchRandom Random { get; }

    /// <summary>The Players, ordered by ascending <see cref="PlayerState.Id"/>.</summary>
    public IReadOnlyList<PlayerState> Players => players;

    /// <summary>
    /// Writes everything that influences future ticks. State added to the match must be added
    /// here too, or two diverged matches would report the same hash.
    /// </summary>
    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Tick);
        Random.WriteTo(hasher);
        Map.WriteTo(hasher);

        hasher.Write(players.Count);

        foreach (var player in players)
        {
            player.WriteTo(hasher);
        }
    }
}

public sealed class MapState
{
    internal MapState(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Width in Cells.</summary>
    public int Width { get; }

    /// <summary>Height in Cells.</summary>
    public int Height { get; }

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Width);
        hasher.Write(Height);
    }
}

public sealed class PlayerState
{
    internal PlayerState(PlayerId id, FactionId faction)
    {
        Id = id;
        Faction = faction;
    }

    public PlayerId Id { get; }

    public FactionId Faction { get; }

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Faction.Value);
    }
}
