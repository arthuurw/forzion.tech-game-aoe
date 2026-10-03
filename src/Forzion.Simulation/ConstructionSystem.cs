namespace Forzion.Simulation;

/// <summary>
/// Puts the work of every Villager building a construction site into it: one tick of work
/// per Villager standing beside the site, so Villagers building together add their work.
/// A site that receives all the work its kind takes is complete, and its builders stand idle.
/// </summary>
internal sealed class ConstructionSystem : ISystem
{
    /// <summary>
    /// Sends the Villager walking up to the site to build it. It stops gathering and keeps
    /// whatever it carries.
    /// </summary>
    public static void Build(MapState map, UnitState villager, BuildingState site)
    {
        villager.StopGathering();
        villager.ConstructionSite = site.Id;
        WalkUpTo(map, villager, site);
    }

    /// <summary>
    /// Sends the Villager walking up to the site, to the Cell it can reach that is nearest in
    /// a straight line to the Cell of the footprint nearest to where it stands, as
    /// <see cref="Pathfinder.FindPath"/> picks it.
    /// </summary>
    public static void WalkUpTo(MapState map, UnitState villager, BuildingState site) =>
        MovementSystem.WalkTo(map, villager, site.NearestCellTo(villager.Position.Cell));

    /// <summary>
    /// Releases the Villagers building a site that has left the match: they stop building and
    /// stand idle on the Cell they are in, keeping whatever they carry.
    /// </summary>
    public static void ReleaseBuilders(MatchState state, EntityId site)
    {
        foreach (var unit in state.Units)
        {
            if (unit.ConstructionSite == site)
            {
                unit.StopBuilding();
                MovementSystem.WalkTo(state.Map, unit, unit.Position.Cell);
            }
        }
    }

    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var unit in state.Units)
        {
            if (unit.ConstructionSite is not { } id || unit.IsMoving)
            {
                continue;
            }

            var site = state.FindBuilding(id)!;

            if (site.IsBeside(unit.Position.Cell))
            {
                Work(context, site);
            }
            else
            {
                // Walked as far as it could and still short of the site: it cannot be reached.
                unit.StopBuilding();
            }
        }
    }

    private static void Work(TickContext context, BuildingState site)
    {
        site.BuildProgress++;

        if (!site.IsComplete)
        {
            return;
        }

        context.Emit(new BuildingCompleted(site.Id));

        foreach (var unit in context.State.Units)
        {
            if (unit.ConstructionSite == site.Id)
            {
                unit.StopBuilding();
            }
        }
    }
}
