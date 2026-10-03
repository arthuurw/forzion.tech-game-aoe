namespace Forzion.Simulation;

/// <summary>
/// The fixed script an AI Player follows (ADR 0003). Each tick it reads the state as any
/// Player sees it and answers with the commands a human could give: no rule of its own, no
/// Resources it has not gathered, nothing a human cannot see. Its only random draws come from
/// the match's generator, so a match with AI Players replays exactly.
/// </summary>
/// <remarks>
/// The script keeps no memory between ticks: every decision is read from the state of the
/// moment, so there is nothing of its own to cover in the state hash, and an order the match
/// refused, or a unit lost, is simply decided again from what is there.
/// </remarks>
internal sealed class AiScript
{
    private static readonly ResourceKind[] ResourceKinds = Enum.GetValues<ResourceKind>();

    private readonly MatchState state;
    private readonly PlayerState player;
    private readonly List<Command> commands = [];

    // What the Player has left to spend this tick: its Resources less the cost of the commands
    // already decided, which the match only charges once it applies them.
    private readonly int[] budget;

    // Villagers given a job this tick, which no later decision of the same tick takes from them.
    private readonly HashSet<EntityId> busy = [];

    private AiScript(MatchState state, PlayerState player)
    {
        this.state = state;
        this.player = player;
        budget = ResourceKinds.Select(player.AmountOf).ToArray();
    }

    /// <summary>The commands the AI Player gives this tick, in the order it gives them.</summary>
    public static IReadOnlyList<Command> Decide(MatchState state, PlayerState player)
    {
        var script = new AiScript(state, player);

        script.BuildHouse();
        script.BuildBarracks();
        script.TrainVillager();
        script.SendIdleVillagersToGather();

        return script.commands;
    }

    /// <summary>
    /// Places a House once the population limit is within <see cref="Balance.AiPopulationHeadroom"/>
    /// of the population, one House at a time.
    /// </summary>
    private void BuildHouse()
    {
        if (state.PopulationLimitOf(player.Id) - state.PopulationOf(player.Id) > Balance.AiPopulationHeadroom
            || OwnBuildings(BuildingKind.House).Any(house => !house.IsComplete))
        {
            return;
        }

        Place(BuildingKind.House);
    }

    /// <summary>
    /// Places the Player's one Barracks once it has <see cref="Balance.AiVillagersBeforeBarracks"/>
    /// Villagers, and again whenever it has none left.
    /// </summary>
    private void BuildBarracks()
    {
        if (OwnBuildings(BuildingKind.Barracks).Any()
            || OwnUnits().Count(unit => unit.Kind == UnitKind.Villager) < Balance.AiVillagersBeforeBarracks)
        {
            return;
        }

        Place(BuildingKind.Barracks);
    }

    /// <summary>
    /// Places a construction site of the given kind near the Town Center with the nearest
    /// Villagers as builders, when what it costs is still in this tick's budget and there is a
    /// place for it.
    /// </summary>
    private void Place(BuildingKind kind)
    {
        var builders = OwnUnits()
            .Where(unit => unit.Kind == UnitKind.Villager && unit.ConstructionSite is null && !busy.Contains(unit.Id))
            .ToList();

        if (builders.Count == 0 || DrawOrigin(kind) is not { } origin || !Spend(Balance.BuildingCost(kind)))
        {
            return;
        }

        var nearest = builders
            .OrderBy(unit => SquaredDistance(unit.Position.Cell, origin))
            .ThenBy(unit => unit.Id.Value)
            .Take(Balance.AiBuilders(kind))
            .Select(unit => unit.Id)
            .ToList();

        busy.UnionWith(nearest);
        commands.Add(new PlaceBuildingCommand(player.Id, kind, origin, nearest));
    }

    /// <summary>
    /// The origin of a footprint of the given kind drawn from the match's generator among the
    /// <see cref="Balance.AiPlacementChoices"/> nearest to the Town Center, within
    /// <see cref="Balance.AiBuildingReach"/> Cells of it, or null when there is none. Only
    /// footprints ringed by free Cells count, so what the AI builds never walls in a unit, a
    /// source or another building. Between origins equally near, the one with the lowest
    /// Cell index comes first.
    /// </summary>
    private CellPosition? DrawOrigin(BuildingKind kind)
    {
        var home = Home();
        var size = Balance.BuildingSize(kind);
        var reach = Balance.AiBuildingReach;
        var origins = new List<(CellPosition Origin, int Distance)>();

        // Row by row, from the lowest: ascending Cell index, which the stable sort below keeps among equals.
        for (var y = home.Y - reach; y <= home.Y + reach; y++)
        {
            for (var x = home.X - reach; x <= home.X + reach; x++)
            {
                var origin = new CellPosition(x, y);

                if (state.CanPlace(kind, origin) && IsRingedByFreeCells(origin, size))
                {
                    origins.Add((origin, SquaredDistance(new CellPosition(x + (size / 2), y + (size / 2)), home)));
                }
            }
        }

        if (origins.Count == 0)
        {
            return null;
        }

        var nearest = origins.OrderBy(each => each.Distance).Take(Balance.AiPlacementChoices).ToList();

        return nearest[state.Random.NextInt(nearest.Count)].Origin;
    }

    /// <summary>Whether every Cell around the footprint, by a side or by a corner, is inside the map and free.</summary>
    private bool IsRingedByFreeCells(CellPosition origin, int size)
    {
        for (var y = origin.Y - 1; y <= origin.Y + size; y++)
        {
            for (var x = origin.X - 1; x <= origin.X + size; x++)
            {
                var inside = x >= origin.X && x < origin.X + size && y >= origin.Y && y < origin.Y + size;

                if (!inside && !state.Map.IsFree(new CellPosition(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Puts a Villager in training at the Town Center while it trains none and the Player has
    /// fewer than <see cref="Balance.AiVillagers"/>, room in its population and the Food.
    /// </summary>
    private void TrainVillager()
    {
        if (TownCenter() is not { IsComplete: true, TrainingQueue.Count: 0 } townCenter
            || OwnUnits().Count(unit => unit.Kind == UnitKind.Villager) >= Balance.AiVillagers)
        {
            return;
        }

        Train(townCenter, UnitKind.Villager);
    }

    /// <summary>
    /// Puts a unit in the building's training queue, when the Player has room in its
    /// population and what it costs is still in this tick's budget.
    /// </summary>
    private void Train(BuildingState building, UnitKind kind)
    {
        if (state.PopulationOf(player.Id) >= state.PopulationLimitOf(player.Id) || !Spend(Balance.UnitCost(kind)))
        {
            return;
        }

        commands.Add(new TrainCommand(player.Id, building.Id, kind));
    }

    /// <summary>Takes the cost from this tick's budget, when it covers it.</summary>
    private bool Spend(Cost cost)
    {
        if (ResourceKinds.Any(kind => budget[(int)kind] < cost.AmountOf(kind)))
        {
            return false;
        }

        foreach (var kind in ResourceKinds)
        {
            budget[(int)kind] -= cost.AmountOf(kind);
        }

        return true;
    }

    /// <summary>
    /// Sends each idle Villager, in ID order, to gather the Resource whose gatherers are fewest
    /// for its share (<see cref="Balance.AiGatherShare"/>), from a source drawn among the
    /// nearest of that Resource to the Town Center.
    /// </summary>
    private void SendIdleVillagersToGather()
    {
        var gatherers = new int[ResourceKinds.Length];

        foreach (var unit in OwnUnits())
        {
            if (unit.GatherSource is { } id && state.FindResourceSource(id) is { } source)
            {
                gatherers[(int)source.Kind]++;
            }
        }

        foreach (var villager in OwnUnits().Where(unit => IsIdleVillager(unit) && !busy.Contains(unit.Id)).ToList())
        {
            if (LeastGatheredKind(gatherers) is not { } kind)
            {
                return;
            }

            var source = DrawSourceNearHome(kind);
            gatherers[(int)kind]++;
            commands.Add(new GatherCommand(player.Id, [villager.Id], source.Id));
        }
    }

    /// <summary>
    /// The Resource that still has sources on the map whose gatherers are fewest for its share;
    /// between Resources level with one another, the first. Null when the map has no source left.
    /// </summary>
    private ResourceKind? LeastGatheredKind(int[] gatherers)
    {
        ResourceKind? least = null;

        foreach (var kind in ResourceKinds)
        {
            if (!state.ResourceSources.Any(source => source.Kind == kind))
            {
                continue;
            }

            // Compares gatherers / share across Resources without fractions.
            if (least is not { } other
                || gatherers[(int)kind] * Balance.AiGatherShare(other) < gatherers[(int)other] * Balance.AiGatherShare(kind))
            {
                least = kind;
            }
        }

        return least;
    }

    /// <summary>
    /// A source of the Resource drawn from the match's generator among the
    /// <see cref="Balance.AiSourceChoices"/> nearest to the Town Center; between sources equally
    /// near, the one with the lowest ID comes first. There must be one.
    /// </summary>
    private ResourceSourceState DrawSourceNearHome(ResourceKind kind)
    {
        var home = Home();
        var nearest = state.ResourceSources
            .Where(source => source.Kind == kind)
            .OrderBy(source => SquaredDistance(source.Cell, home))
            .ThenBy(source => source.Id.Value)
            .Take(Balance.AiSourceChoices)
            .ToList();

        return nearest[state.Random.NextInt(nearest.Count)];
    }

    /// <summary>The Cell the Player's Town Center is centred on, or the map's centre once it has none.</summary>
    private CellPosition Home() =>
        TownCenter() is { } townCenter
            ? new CellPosition(townCenter.Origin.X + (townCenter.Width / 2), townCenter.Origin.Y + (townCenter.Height / 2))
            : new CellPosition(state.Map.Width / 2, state.Map.Height / 2);

    private BuildingState? TownCenter() =>
        state.Buildings.FirstOrDefault(building => building.Owner == player.Id && building.Kind == BuildingKind.TownCenter);

    private IEnumerable<BuildingState> OwnBuildings(BuildingKind kind) =>
        state.Buildings.Where(building => building.Owner == player.Id && building.Kind == kind);

    private IEnumerable<UnitState> OwnUnits() => state.Units.Where(unit => unit.Owner == player.Id);

    /// <summary>A Villager with no source, no construction site and nowhere to walk.</summary>
    private static bool IsIdleVillager(UnitState unit) =>
        unit.Kind == UnitKind.Villager && unit.GatherSource is null && unit.ConstructionSite is null && !unit.IsMoving;

    private static int SquaredDistance(CellPosition a, CellPosition b) =>
        ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));
}
