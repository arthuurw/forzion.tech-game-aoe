namespace Forzion.Simulation;

/// <summary>
/// The state of a match: plain data identified by ID, read-only from outside the simulation.
/// Only the simulation's own tick changes it. Collections are ordered by ascending ID.
/// </summary>
public sealed class MatchState
{
    private readonly List<PlayerState> players;
    private readonly List<ResourceSourceState> resourceSources = [];
    private readonly List<BuildingState> buildings = [];
    private readonly List<UnitState> units = [];
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

        var generated = MapGenerator.Generate(Map, Random);

        foreach (var (cell, kind) in generated.Sources)
        {
            AddResourceSource(kind, cell, Balance.SourceAmount(kind));
        }

        foreach (var player in players)
        {
            PlaceStartingEntities(player.Id, generated.Homes[player.Id.Value - 1]);
        }

        // After every Player's usual start, so extra units never shift the IDs of the rest.
        foreach (var player in players)
        {
            PlaceExtraUnits(player.Id, config.Players[player.Id.Value - 1].ExtraUnits ?? []);
        }
    }

    /// <summary>Number of ticks simulated so far.</summary>
    public int Tick { get; internal set; }

    public MapState Map { get; }

    internal MatchRandom Random { get; }

    /// <summary>The Players, ordered by ascending <see cref="PlayerState.Id"/>.</summary>
    public IReadOnlyList<PlayerState> Players => players;

    /// <summary>The resource sources, ordered by ascending <see cref="ResourceSourceState.Id"/>.</summary>
    public IReadOnlyList<ResourceSourceState> ResourceSources => resourceSources;

    /// <summary>The buildings, ordered by ascending <see cref="BuildingState.Id"/>.</summary>
    public IReadOnlyList<BuildingState> Buildings => buildings;

    /// <summary>The units, ordered by ascending <see cref="UnitState.Id"/>.</summary>
    public IReadOnlyList<UnitState> Units => units;

    /// <summary>Whether the match has ended: at most one of its Players remains undefeated.</summary>
    public bool IsOver { get; private set; }

    /// <summary>The Player who won the match, or null while it goes on or when it ended without a winner.</summary>
    public PlayerId? Winner { get; private set; }

    internal void End(PlayerId? winner)
    {
        IsOver = true;
        Winner = winner;
    }

    /// <summary>The Player with the given ID, or null when the match has no such Player.</summary>
    internal PlayerState? FindPlayer(PlayerId id) =>
        id.Value >= 1 && id.Value <= players.Count ? players[id.Value - 1] : null;

    /// <summary>The unit with the given ID, or null when the match has no such unit.</summary>
    internal UnitState? FindUnit(EntityId id) => units.Find(unit => unit.Id == id);

    /// <summary>The building with the given ID, or null when the match has no such building.</summary>
    internal BuildingState? FindBuilding(EntityId id) => buildings.Find(building => building.Id == id);

    /// <summary>
    /// Adds a resource source and marks its Cell occupied. IDs only grow, so appending keeps
    /// the collection in ID order.
    /// </summary>
    internal ResourceSourceState AddResourceSource(ResourceKind kind, CellPosition cell, int amount)
    {
        var source = new ResourceSourceState(NextEntityId(), kind, cell, amount);

        Map[cell] = CellKind.ResourceSource;
        resourceSources.Add(source);

        return source;
    }

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

    /// <summary>Adds a unit. IDs only grow, so appending keeps the collection in ID order.</summary>
    internal UnitState AddUnit(PlayerId owner, UnitKind kind, MapPosition position)
    {
        var unit = new UnitState(NextEntityId(), owner, kind, position);

        units.Add(unit);

        return unit;
    }

    /// <summary>
    /// Removes every unit and building left without hit points, freeing the Cells the
    /// buildings occupied, and returns their IDs in ascending order.
    /// </summary>
    internal List<EntityId> RemoveDestroyed()
    {
        var destroyedUnits = units.Where(unit => unit.HitPoints <= 0).ToList();
        var destroyedBuildings = buildings.Where(building => building.HitPoints <= 0).ToList();

        foreach (var building in destroyedBuildings)
        {
            for (var y = building.Origin.Y; y < building.Origin.Y + building.Height; y++)
            {
                for (var x = building.Origin.X; x < building.Origin.X + building.Width; x++)
                {
                    Map[new CellPosition(x, y)] = CellKind.Free;
                }
            }
        }

        units.RemoveAll(unit => unit.HitPoints <= 0);
        buildings.RemoveAll(building => building.HitPoints <= 0);

        return destroyedUnits.Select(unit => unit.Id)
            .Concat(destroyedBuildings.Select(building => building.Id))
            .OrderBy(id => id.Value)
            .ToList();
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
        hasher.Write(resourceSources.Count);

        foreach (var source in resourceSources)
        {
            source.WriteTo(hasher);
        }

        hasher.Write(buildings.Count);

        foreach (var building in buildings)
        {
            building.WriteTo(hasher);
        }

        hasher.Write(units.Count);

        foreach (var unit in units)
        {
            unit.WriteTo(hasher);
        }

        hasher.Write(IsOver);
        hasher.Write(Winner?.Value ?? 0);
    }

    private EntityId NextEntityId() => new(++lastEntityId);

    private void PlaceExtraUnits(PlayerId player, IReadOnlyList<StartingUnit> extraUnits)
    {
        foreach (var extra in extraUnits)
        {
            if (!Map.Contains(extra.Cell) || Map[extra.Cell] != CellKind.Free)
            {
                throw new ArgumentException(
                    $"Player {player.Value} has an extra unit on {extra.Cell}, which is not a free Cell of the map.",
                    "config");
            }

            AddUnit(player, extra.Kind, MapPosition.CentreOf(extra.Cell));
        }
    }

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

        // The Villagers line up on the row just outside the Town Center, on the side facing
        // the centre of the map, so that the two Players' lines mirror each other.
        var towardsCentre = home.Y < Map.Height - 1 - home.Y ? 1 : -1;

        for (var offset = -reach; offset < Balance.StartingVillagers - reach; offset++)
        {
            var cell = new CellPosition(home.X + (offset * towardsCentre), home.Y + ((reach + 1) * towardsCentre));

            AddUnit(player, UnitKind.Villager, MapPosition.CentreOf(cell));
        }
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
