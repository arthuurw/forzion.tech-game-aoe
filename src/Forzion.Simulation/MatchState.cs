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

    /// <summary>
    /// How many units the Player may have at once: what its complete buildings provide, the
    /// Town Center a base and each House more. Construction sites provide nothing.
    /// </summary>
    public int PopulationLimitOf(PlayerId player) => buildings
        .Where(building => building.Owner == player && building.IsComplete)
        .Sum(building => Balance.PopulationProvided(building.Kind));

    /// <summary>The Player with the given ID, or null when the match has no such Player.</summary>
    internal PlayerState? FindPlayer(PlayerId id) =>
        id.Value >= 1 && id.Value <= players.Count ? players[id.Value - 1] : null;

    /// <summary>The unit with the given ID, or null when the match has no such unit.</summary>
    internal UnitState? FindUnit(EntityId id) => units.Find(unit => unit.Id == id);

    /// <summary>The building with the given ID, or null when the match has no such building.</summary>
    internal BuildingState? FindBuilding(EntityId id) => buildings.Find(building => building.Id == id);

    /// <summary>The resource source with the given ID, or null when the match has no such source.</summary>
    internal ResourceSourceState? FindResourceSource(EntityId id) => resourceSources.Find(source => source.Id == id);

    /// <summary>
    /// Adds a resource source on a free Cell and marks the Cell occupied. IDs only grow, so
    /// appending keeps the collection in ID order.
    /// </summary>
    internal ResourceSourceState AddResourceSource(ResourceKind kind, CellPosition cell, int amount)
    {
        var source = new ResourceSourceState(NextEntityId(), kind, cell, amount);

        Map[cell] = CellKind.ResourceSource;
        resourceSources.Add(source);

        return source;
    }

    /// <summary>Removes a resource source and frees its Cell. Removing keeps the collection in ID order.</summary>
    internal void RemoveResourceSource(ResourceSourceState source)
    {
        resourceSources.Remove(source);
        Map[source.Cell] = CellKind.Free;
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

    /// <summary>
    /// Whether every Cell of the footprint a building of the given kind would have from
    /// <paramref name="origin"/> is inside the map, free and has no unit standing on it.
    /// </summary>
    internal bool CanPlace(BuildingKind kind, CellPosition origin)
    {
        var size = Balance.BuildingSize(kind);

        for (var y = origin.Y; y < origin.Y + size; y++)
        {
            for (var x = origin.X; x < origin.X + size; x++)
            {
                var cell = new CellPosition(x, y);

                if (!Map.IsFree(cell))
                {
                    return false;
                }
            }
        }

        // A unit inside the footprint would be walled in by it.
        return !units.Any(unit =>
            unit.Position.Cell.X >= origin.X && unit.Position.Cell.X < origin.X + size
            && unit.Position.Cell.Y >= origin.Y && unit.Position.Cell.Y < origin.Y + size);
    }

    /// <summary>Adds a unit. IDs only grow, so appending keeps the collection in ID order.</summary>
    internal UnitState AddUnit(PlayerId owner, UnitKind kind, MapPosition position)
    {
        var unit = new UnitState(NextEntityId(), owner, kind, position);

        units.Add(unit);

        return unit;
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
    }

    private EntityId NextEntityId() => new(++lastEntityId);

    /// <summary>What a Player starts the match with, around the Cell the map gave as home.</summary>
    private void PlaceStartingEntities(PlayerId player, CellPosition home)
    {
        var reach = Balance.TownCenterSize / 2;

        var townCenter = AddBuilding(
            player,
            BuildingKind.TownCenter,
            new CellPosition(home.X - reach, home.Y - reach),
            Balance.TownCenterSize,
            Balance.TownCenterSize);

        townCenter.BuildProgress = townCenter.BuildTime;

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

/// <summary>A Player of the match, human or AI, as the state knows them.</summary>
public sealed class PlayerState
{
    // How much of each Resource the Player has, indexed by ResourceKind.
    private readonly int[] resources = new int[Enum.GetValues<ResourceKind>().Length];

    internal PlayerState(PlayerId id, FactionId faction)
    {
        Id = id;
        Faction = faction;
    }

    /// <summary>The Player's ID, assigned from 1 upward in configuration order.</summary>
    public PlayerId Id { get; }

    /// <summary>The Faction the Player controls, as configured. It never changes during the match.</summary>
    public FactionId Faction { get; }

    /// <summary>Whether the Player has been defeated. A defeated Player stays in the state.</summary>
    public bool IsDefeated { get; internal set; }

    /// <summary>How much of the given Resource the Player has.</summary>
    public int AmountOf(ResourceKind kind) => resources[(int)kind];

    internal void Receive(ResourceKind kind, int amount) => resources[(int)kind] += amount;

    /// <summary>Whether the Player has at least the cost in each Resource.</summary>
    internal bool CanAfford(Cost cost) =>
        Enum.GetValues<ResourceKind>().All(kind => AmountOf(kind) >= cost.AmountOf(kind));

    /// <summary>Takes the cost from the Player, who must be able to afford it.</summary>
    internal void Pay(Cost cost)
    {
        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            resources[(int)kind] -= cost.AmountOf(kind);
        }
    }

    internal void WriteTo(StateHasher hasher)
    {
        hasher.Write(Id.Value);
        hasher.Write(Faction.Value);
        hasher.Write(IsDefeated);

        foreach (var amount in resources)
        {
            hasher.Write(amount);
        }
    }
}
