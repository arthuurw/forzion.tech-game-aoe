namespace Forzion.Simulation;

/// <summary>
/// Sends the Villagers waiting for a way to their job on their way again once the tick may
/// have opened one: a building destroyed or a resource source depleted frees Cells, and a
/// drop-off point completed is a new place to deliver to.
/// </summary>
internal sealed class WaitingSystem : ISystem
{
    public void Run(TickContext context)
    {
        if (context.WaysMayHaveOpened)
        {
            Rerouting.ForStandingJobs(context.State);
        }
    }
}
