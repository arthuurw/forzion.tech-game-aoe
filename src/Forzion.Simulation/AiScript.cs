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

    private AiScript(MatchState state, PlayerState player)
    {
        this.state = state;
        this.player = player;
    }

    /// <summary>The commands the AI Player gives this tick, in the order it gives them.</summary>
    public static IReadOnlyList<Command> Decide(MatchState state, PlayerState player)
    {
        var script = new AiScript(state, player);

        script.SendIdleVillagersToGather();

        return script.commands;
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

        foreach (var villager in OwnUnits().Where(IsIdleVillager).ToList())
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

    private IEnumerable<UnitState> OwnUnits() => state.Units.Where(unit => unit.Owner == player.Id);

    /// <summary>A Villager with no source, no construction site and nowhere to walk.</summary>
    private static bool IsIdleVillager(UnitState unit) =>
        unit.Kind == UnitKind.Villager && unit.GatherSource is null && unit.ConstructionSite is null && !unit.IsMoving;

    private static int SquaredDistance(CellPosition a, CellPosition b) =>
        ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y));
}
