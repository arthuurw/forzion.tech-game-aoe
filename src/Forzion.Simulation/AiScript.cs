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

    // What the Player saves up for its Age Advance, kept out of the budget for soldiers.
    private Cost savings;

    // Whether a soldier was put in training this tick.
    private bool armyGrows;

    // Villagers given a job this tick, which no later decision of the same tick takes from them.
    private readonly HashSet<EntityId> busy = [];

    // Construction sites placed this tick, which the match only adds once it applies the
    // placements: a later placement of the same tick keeps clear of their footprints.
    private readonly List<(BuildingKind Kind, Footprint Footprint)> placed = [];

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

        script.ResumeConstruction();
        script.BuildHouse();
        script.BuildBarracks();
        script.TrainVillager();
        script.AdvanceAge();
        script.TrainSoldier();
        script.SendVillagersToGather();
        script.Attack();

        return script.commands;
    }

    /// <summary>
    /// Sends a Villager to each construction site of the Player that nobody builds, as when its
    /// builders were killed: the one nearest to it among those that build nothing.
    /// </summary>
    private void ResumeConstruction()
    {
        foreach (var site in state.Buildings.Where(building => building.Owner == player.Id && !building.IsComplete))
        {
            if (OwnUnits().Any(unit => unit.ConstructionSite == site.Id))
            {
                continue;
            }

            var builder = VillagersNotBuilding()
                .OrderBy(unit => unit.Position.Cell.SquaredDistanceTo(site.Origin))
                .ThenBy(unit => unit.Id.Value)
                .FirstOrDefault();

            if (builder is null)
            {
                return;
            }

            busy.Add(builder.Id);
            commands.Add(new BuildCommand(player.Id, [builder.Id], site.Id));
        }
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

        PlaceNearHome(BuildingKind.House);
    }

    /// <summary>
    /// Places the Player's one Barracks once it has <see cref="Balance.AiVillagersBeforeBarracks"/>
    /// Villagers, and again whenever it has none left.
    /// </summary>
    private void BuildBarracks()
    {
        if (OwnBuildings(BuildingKind.Barracks).Any()
            || VillagerCount() < Balance.AiVillagersBeforeBarracks)
        {
            return;
        }

        PlaceNearHome(BuildingKind.Barracks);
    }

    /// <summary>
    /// Places a construction site of the given kind within <see cref="Balance.AiBuildingReach"/>
    /// of the Town Center, with the Villagers nearest to it that build nothing as builders.
    /// </summary>
    private void PlaceNearHome(BuildingKind kind)
    {
        Place(kind, Home(), Balance.AiBuildingReach, VillagersNotBuilding().ToList());
    }

    /// <summary>
    /// Places a construction site of the given kind within <paramref name="reach"/> of
    /// <paramref name="centre"/>, with the candidates nearest to it as builders, when there is a
    /// candidate, what it costs is still in this tick's budget and there is a place for it.
    /// Returns whether it did.
    /// </summary>
    private bool Place(BuildingKind kind, CellPosition centre, int reach, IReadOnlyList<UnitState> candidates)
    {
        var cost = Match.BuildingCost(kind);

        // Checked before the place is drawn, so a Player saving up does not use up draws.
        if (candidates.Count == 0 || !CanSpend(cost) || DrawOrigin(kind, centre, reach) is not { } origin)
        {
            return false;
        }

        var builders = candidates
            .OrderBy(unit => unit.Position.Cell.SquaredDistanceTo(origin))
            .ThenBy(unit => unit.Id.Value)
            .Take(Balance.AiBuilders(kind))
            .Select(unit => unit.Id)
            .ToList();

        Spend(cost);
        busy.UnionWith(builders);
        placed.Add((kind, Footprint.Of(kind, origin)));
        commands.Add(new PlaceBuildingCommand(player.Id, kind, origin, builders));

        return true;
    }

    /// <summary>
    /// The origin of a footprint of the given kind drawn from the match's generator among the
    /// <see cref="Balance.AiPlacementChoices"/> nearest to <paramref name="centre"/>, within
    /// <paramref name="reach"/> Cells of it along either axis, or null when there is none. Only
    /// footprints ringed by free Cells count, so what the AI builds never walls in a unit, a
    /// source or another building. Between origins equally near, the one with the lowest
    /// Cell index comes first.
    /// </summary>
    private CellPosition? DrawOrigin(BuildingKind kind, CellPosition centre, int reach)
    {
        var origins = new List<(CellPosition Origin, int Distance)>();

        for (var y = centre.Y - reach; y <= centre.Y + reach; y++)
        {
            for (var x = centre.X - reach; x <= centre.X + reach; x++)
            {
                var origin = new CellPosition(x, y);
                var footprint = Footprint.Of(kind, origin);

                if (state.CanPlace(kind, origin) && IsRingedByFreeCells(footprint) && !IsNearPlaced(footprint))
                {
                    origins.Add((origin, footprint.Centre.SquaredDistanceTo(centre)));
                }
            }
        }

        if (origins.Count == 0)
        {
            return null;
        }

        // Row then column breaks ties: ascending Cell index.
        var nearest = origins
            .OrderBy(each => each.Distance)
            .ThenBy(each => each.Origin.Y)
            .ThenBy(each => each.Origin.X)
            .Take(Balance.AiPlacementChoices)
            .ToList();

        return nearest[state.Random.NextInt(nearest.Count)].Origin;
    }

    /// <summary>
    /// Whether the footprint, or a Cell around it, overlaps a footprint placed this tick: the
    /// two would touch or overlap once both are applied.
    /// </summary>
    private bool IsNearPlaced(Footprint footprint) =>
        placed.Any(other => footprint.WithRing().Overlaps(other.Footprint));

    /// <summary>Whether every Cell around the footprint, by a side or by a corner, is inside the map and free.</summary>
    private bool IsRingedByFreeCells(Footprint footprint) => footprint.WithRing().Cells
        .Where(cell => !footprint.Contains(cell))
        .All(state.Map.IsFree);

    /// <summary>
    /// Puts a Villager in training at the Town Center while it trains none and the Player has
    /// fewer than <see cref="Balance.AiVillagers"/>, room in its population and the Food.
    /// </summary>
    private void TrainVillager()
    {
        if (TownCenter() is not { IsComplete: true, TrainingQueue.Count: 0 } townCenter
            || VillagerCount() >= Balance.AiVillagers)
        {
            return;
        }

        Train(townCenter, UnitKind.Villager);
    }

    /// <summary>
    /// Orders the Age Advance at the Town Center once the Player has a complete Barracks and an
    /// army of <see cref="Balance.AiArmyBeforeAdvance"/>, and is in neither the last Age of its
    /// Faction nor an Age Advance already. Until it can pay, it saves up: a soldier is trained
    /// only when it leaves what the advance takes of each Resource the soldier costs. It saves
    /// up only while every Resource it is short of can still be gathered somewhere on the map.
    /// </summary>
    private void AdvanceAge()
    {
        if (player.NextAge is not { } nextAge
            || TownCenter() is not { IsComplete: true, AgeAdvanceProgress: null } townCenter
            || !OwnBuildings(BuildingKind.Barracks).Any(barracks => barracks.IsComplete)
            || Army().Count() < Balance.AiArmyBeforeAdvance)
        {
            return;
        }

        var cost = nextAge.AdvanceCost;

        if (Spend(cost))
        {
            commands.Add(new AgeAdvanceCommand(player.Id, townCenter.Id));
        }
        else if (ResourceKinds.All(kind =>
                     budget[(int)kind] >= cost.AmountOf(kind) || state.ResourceSources.Any(source => source.Kind == kind)))
        {
            savings = cost;
        }
    }

    /// <summary>
    /// Puts a soldier in training at a complete Barracks while it trains none: of a kind drawn
    /// from the match's generator among the soldiers this tick's budget covers, those unlocked
    /// by the latest Age only. A Player short of what the strongest soldiers take trains the
    /// others rather than nothing, so what is left once a Resource runs dry still arms it.
    /// </summary>
    private void TrainSoldier()
    {
        if (OwnBuildings(BuildingKind.Barracks).FirstOrDefault(barracks => barracks.IsComplete && barracks.TrainingQueue.Count == 0)
                is not { } barracks
            || IsPopulationFull())
        {
            return;
        }

        var affordable = AffordableSoldiersOfTheLatestAge();

        // Drawn only when there is something to train, so a Player saving up does not use up draws.
        if (affordable.Count > 0)
        {
            Train(barracks, affordable[state.Random.NextInt(affordable.Count)]);
            armyGrows = true;
        }
    }

    /// <summary>
    /// The kinds of soldier this tick's budget covers, beyond the savings, among those unlocked by
    /// the latest Age, up to the Player's, that unlocks any it covers; in ascending kind order.
    /// </summary>
    private List<UnitKind> AffordableSoldiersOfTheLatestAge() => Enumerable.Range(1, player.Age)
        .Reverse()
        .Select(age => player.Faction.AgeAt(age).Units
            .Where(kind => Balance.Of(kind).Attack is not null && CanSpend(Balance.Of(kind).Cost, savings))
            .Order()
            .ToList())
        .FirstOrDefault(soldiers => soldiers.Count > 0) ?? [];

    /// <summary>
    /// Sends every soldier attacking nothing against the nearest enemy Town Center once there are
    /// <see cref="Balance.AiAttackArmySize"/> of them, or once the army has stopped growing: no
    /// Villager gathers and no Barracks trains, now or this tick, so nothing will change until
    /// the soldiers move. Between Town Centers equally near, the one with the lowest ID.
    /// Soldiers already fighting are left to it.
    /// </summary>
    private void Attack()
    {
        var unengaged = Army().Where(unit => unit.Target is null).Select(unit => unit.Id).ToList();
        var home = Home();
        var target = state.Buildings
            .Where(building => building.Owner != player.Id && building.Kind == BuildingKind.TownCenter)
            .OrderBy(townCenter => townCenter.Origin.SquaredDistanceTo(home))
            .ThenBy(townCenter => townCenter.Id.Value)
            .FirstOrDefault();
        var stopped = !armyGrows
            && !OwnUnits().Any(unit => unit.GatherSource is not null)
            && !OwnBuildings(BuildingKind.Barracks).Any(barracks => barracks.TrainingQueue.Count > 0);

        if (target is not null && unengaged.Count > 0 && (unengaged.Count >= Balance.AiAttackArmySize || stopped))
        {
            commands.Add(new AttackCommand(player.Id, unengaged, target.Id));
        }
    }

    /// <summary>
    /// Puts a unit in the building's training queue, when the Player has room in its
    /// population and what it costs is still in this tick's budget.
    /// </summary>
    private void Train(BuildingState building, UnitKind kind)
    {
        if (IsPopulationFull() || !Spend(Balance.Of(kind).Cost))
        {
            return;
        }

        commands.Add(new TrainCommand(player.Id, building.Id, kind));
    }

    /// <summary>
    /// Whether this tick's budget covers the cost and, for each Resource the cost takes, still
    /// leaves what is kept aside. A Resource the cost does not take is not touched, so it is not
    /// checked against what is kept aside.
    /// </summary>
    private bool CanSpend(Cost cost, Cost keptAside = default) => ResourceKinds.All(kind =>
        budget[(int)kind] >= cost.AmountOf(kind)
        && (cost.AmountOf(kind) == 0 || budget[(int)kind] - cost.AmountOf(kind) >= keptAside.AmountOf(kind)));

    /// <summary>Takes the cost from this tick's budget, when it covers it.</summary>
    private bool Spend(Cost cost)
    {
        if (!CanSpend(cost))
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
    /// Sends each Villager that needs a new job (<see cref="NeedsNewJob"/>), in ID order, to
    /// gather the Resource whose gatherers are fewest for its share
    /// (<see cref="Balance.AiGatherShare"/>), from a source drawn among the nearest of that
    /// Resource to the Player's drop-off points. When even that source lies farther than
    /// <see cref="Balance.AiStorehouseDistance"/> from all of them, the Villager first places a
    /// Storehouse by it, one at a time, and is sent to gather once it is Idle again.
    /// </summary>
    private void SendVillagersToGather()
    {
        var gatherers = new int[ResourceKinds.Length];

        foreach (var unit in OwnUnits())
        {
            if (unit.GatherSource is { } id && state.FindResourceSource(id) is { } source)
            {
                gatherers[(int)source.Kind]++;
            }
        }

        foreach (var villager in OwnUnits().Where(unit => NeedsNewJob(unit) && !busy.Contains(unit.Id)).ToList())
        {
            if (LeastGatheredKind(gatherers) is not { } kind)
            {
                return;
            }

            var source = DrawSource(kind);
            gatherers[(int)kind]++;

            if (DropOffDistance(source.Cell) > Balance.AiStorehouseDistance
                && !OwnBuildings(BuildingKind.Storehouse).Any(storehouse => !storehouse.IsComplete)
                && !placed.Any(site => site.Kind == BuildingKind.Storehouse)
                && Place(BuildingKind.Storehouse, source.Cell, Balance.AiStorehouseReach, [villager]))
            {
                continue;
            }

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
    /// <see cref="Balance.AiSourceChoices"/> nearest to the Player's drop-off points, leaving out
    /// those more than <see cref="Balance.AiSourceSlack"/> Cells farther than the nearest; between
    /// sources equally near, the one with the lowest ID comes first. There must be one.
    /// </summary>
    private ResourceSourceState DrawSource(ResourceKind kind)
    {
        var sources = state.ResourceSources
            .Where(source => source.Kind == kind)
            .Select(source => (Source: source, Distance: DropOffDistance(source.Cell)))
            .OrderBy(each => each.Distance)
            .ThenBy(each => each.Source.Id.Value)
            .ToList();
        var nearest = sources
            .TakeWhile(each => each.Distance <= sources[0].Distance + Balance.AiSourceSlack)
            .Take(Balance.AiSourceChoices)
            .ToList();

        return nearest[state.Random.NextInt(nearest.Count)].Source;
    }

    /// <summary>
    /// Distance in king's moves from the Cell to the nearest of the Player's Town Center and
    /// Storehouses, construction sites included: a Storehouse being built already counts.
    /// <see cref="int.MaxValue"/> when the Player has none.
    /// </summary>
    private int DropOffDistance(CellPosition cell) => state.Buildings
        .Where(building => building.Owner == player.Id && building.Kind is BuildingKind.TownCenter or BuildingKind.Storehouse)
        .Select(building => cell.KingDistanceTo(building.Footprint.NearestCellTo(cell)))
        .DefaultIfEmpty(int.MaxValue)
        .Min();

    /// <summary>
    /// The Cell the Player's own Town Center is centred on, or the map's centre once it has none:
    /// what the AI builds around and measures the enemy Town Centers from. Not a term of the
    /// game, only the AI's name for the area around its own Town Center.
    /// </summary>
    private CellPosition Home() =>
        TownCenter() is { } townCenter
            ? townCenter.Footprint.Centre
            : new CellPosition(state.Map.Width / 2, state.Map.Height / 2);

    private BuildingState? TownCenter() => state.TownCenterOf(player.Id);

    private IEnumerable<BuildingState> OwnBuildings(BuildingKind kind) =>
        state.Buildings.Where(building => building.Owner == player.Id && building.Kind == kind);

    /// <summary>The Player's Villagers that build nothing and were given no job this tick.</summary>
    private IEnumerable<UnitState> VillagersNotBuilding() => OwnUnits()
        .Where(unit => unit.Kind == UnitKind.Villager && unit.ConstructionSite is null && !busy.Contains(unit.Id));

    /// <summary>How many Villagers the Player has, those in training left out.</summary>
    private int VillagerCount() => OwnUnits().Count(unit => unit.Kind == UnitKind.Villager);

    /// <summary>Whether the Player's population has reached its population limit, so it can train no more.</summary>
    private bool IsPopulationFull() => state.PopulationOf(player.Id) >= state.PopulationLimitOf(player.Id);

    /// <summary>The Player's units that are not Villagers.</summary>
    private IEnumerable<UnitState> Army() => OwnUnits().Where(unit => unit.Kind != UnitKind.Villager);

    private IEnumerable<UnitState> OwnUnits() => state.Units.Where(unit => unit.Owner == player.Id);

    /// <summary>
    /// Whether the unit is a Villager the AI gives a new job: an Idle one, standing still with
    /// no job, or one Waiting with a job it cannot reach, a source or a construction site with
    /// no way to it. A Waiting Villager is not Idle, but the AI does not leave it to wait until
    /// a way opens; it gives it a new job once it has walked as near to the old one as it can.
    /// </summary>
    private bool NeedsNewJob(UnitState unit) =>
        unit.CanGather
        && !unit.IsMoving
        && ((unit.GatherSource is null && unit.ConstructionSite is null)
            || (unit.GatherPhase == GatherPhase.ToSource && !state.FindResourceSource(unit.GatherSource!.Value)!.IsBeside(unit.Position.Cell))
            || (unit.ConstructionSite is { } site && !state.FindBuilding(site)!.IsBeside(unit.Position.Cell)));
}
