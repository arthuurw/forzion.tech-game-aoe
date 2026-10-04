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
        villager.StartBuilding(site.Id);
        WalkUpToSite(map, villager, site);
    }

    /// <summary>
    /// Sends the Villager walking up to the site, to the free Cell beside it that has the
    /// shortest way to it, whichever side that is; between Cells equally far, the one with the
    /// lowest index. A Villager that cannot reach any Cell beside the site walks to the Cell it
    /// can reach nearest to the site's centre and waits there, keeping the site, until a way
    /// opens.
    /// </summary>
    public static void WalkUpToSite(MapState map, UnitState villager, BuildingState site)
    {
        // The centre, not the side nearest to the Villager: every new way it looks for then aims
        // at the same Cell, wherever it stands by then.
        MovementSystem.WalkToNearestOrTowards(map, villager, site.IsBeside, () => site.Footprint.Centre);
    }

    /// <summary>
    /// Sends the Villager up to the site it builds again, choosing again where it walks as it
    /// did when it set out. False, changing nothing, when it builds no site.
    /// </summary>
    public static bool ChooseWayAgain(MatchState state, UnitState villager)
    {
        if (villager.ConstructionSite is not { } site)
        {
            return false;
        }

        WalkUpToSite(state.Map, villager, state.FindBuilding(site)!);

        return true;
    }

    /// <summary>
    /// Releases the Villagers building a site that is complete or has left the match: they
    /// stop building and stand idle on the Cell they are in, keeping whatever they carry.
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

            // Short of the site, the Villager walked as far as it could and waits for a way.
            if (site.IsBeside(unit.Position.Cell))
            {
                Work(context, site);
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

        if (site.IsDropOffPoint)
        {
            context.NoteWaysMayHaveOpened();
        }

        ReleaseBuilders(context.State, site.Id);
    }
}
