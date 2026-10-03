namespace Forzion.Simulation;

/// <summary>
/// The state of a match: plain data identified by ID, read-only from outside the simulation.
/// Only the simulation's own tick changes it. Collections are ordered by ascending ID.
/// </summary>
public sealed class MatchState
{
    private readonly List<PlayerState> players;
    private readonly List<BuildingState> buildings = [];
    private int lastEntityId;

    internal MatchState(MatchConfig config)
    {
        if (config.Players.Count is < 1 or > MapGenerator.MaximumPlayers)
        {
            throw new ArgumentException(
                $"A match has from 1 to {MapGenerator.MaximumPlayers} Players.", nameof(config));
        }

        if (config.Map.Width < MapGenerator.MinimumSize || config.Map.Height < MapGenerator.MinimumSize)
        {
            throw new ArgumentException(
                $"A map is at least {MapGenerator.MinimumSize} Cells wide and high.", nameof(config));
        }

        Random = new MatchRandom(config.Seed);
        Map = new MapState(config.Map.Width, config.Map.Height);
        players = config.Players
            .Select((player, index) => new PlayerState(new PlayerId(index + 1), player.Faction))
            .ToList();

        var homes = MapGenerator.Generate(Map, Random);

        foreach (var player in players)
        {
            PlaceStartingEntities(player.Id, homes[player.Id.Value - 1]);
        }
    }

    /// <summary>Number of ticks simulated so far.</summary>
    public int Tick { get; internal set; }

    public MapState Map { get; }

    internal MatchRandom Random { get; }

    /// <summary>The Players, ordered by ascending <see cref="PlayerState.Id"/>.</summary>
    public IReadOnlyList<PlayerState> Players => players;

    /// <summary>The buildings, ordered by ascending <see cref="BuildingState.Id"/>.</summary>
    public IReadOnlyList<BuildingState> Buildings => buildings;

    /// <summary>The Player with the given ID, or null when the match has no such Player.</summary>
    internal PlayerState? FindPlayer(PlayerId id) =>
        id.Value >= 1 && id.Value <= players.Count ? players[id.Value - 1] : null;

    /// <summary>
    /// Adds a building on free Cells and marks them occupied. IDs only grow, so appending
    /// keeps the collection in ID order.
    /// </summary>
    internal BuildingState AddBuilding(PlayerId owner, BuildingKind kind, CellPosition origin, int width, int height)
    {
        var building = new BuildingState(NextEntityId(), owner, kind, origin, width, height);

        for (var y = origin.Y; y < origin.Y + height; y++)
        {
            for (var x = origin.X; x < origin.X + width; x++)
            {
                Map[new CellPosition(x, y)] = CellKind.Building;
            }
        }

        buildings.Add(building);

        return building;
    }

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

        hasher.Write(lastEntityId);
        hasher.Write(buildings.Count);

        foreach (var building in buildings)
        {
            building.WriteTo(hasher);
        }
    }

    private EntityId NextEntityId() => new(++lastEntityId);

    /// <summary>What a Player starts the match with, around the Cell the map gave as home.</summary>
    private void PlaceStartingEntities(PlayerId player, CellPosition home)
    {
        var reach = Balance.TownCenterSize / 2;

        AddBuilding(
            player,
            BuildingKind.TownCenter,
            new CellPosition(home.X - reach, home.Y - reach),
            Balance.TownCenterSize,
            Balance.TownCenterSize);
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

    /// <summary>Whether the Player has been defeated. A defeated Player stays in the state.</summary>
    public bool IsDefeated { get; internal set; }

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Faction.Value);
        hasher.Write(IsDefeated);
    }
}
