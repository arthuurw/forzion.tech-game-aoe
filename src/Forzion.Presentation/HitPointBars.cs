using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Which hit point bars the screen shows: one over every unit and building that is selected,
/// and one over every unit and building of any Player that is wounded, so a fight can be
/// read at a glance.
/// </summary>
public static class HitPointBars
{
    /// <summary>The bars to draw this frame, in ascending ID order.</summary>
    /// <param name="driver">Drives the match: its state and where its units are drawn.</param>
    /// <param name="selected">The selected entities; those no longer in the match are skipped.</param>
    public static IReadOnlyList<HitPointBar> Shown(MatchDriver driver, IReadOnlyCollection<EntityId> selected)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(selected);

        var state = driver.Match.State;
        var bars = new List<HitPointBar>();

        foreach (var unit in state.Units)
        {
            if (unit.HitPoints < unit.MaxHitPoints || selected.Contains(unit.Id))
            {
                bars.Add(new HitPointBar(unit.Id, driver.PositionOf(unit), OverBuilding: false, Fill(unit.HitPoints, unit.MaxHitPoints)));
            }
        }

        foreach (var building in state.Buildings)
        {
            if (building.HitPoints < building.MaxHitPoints || selected.Contains(building.Id))
            {
                bars.Add(new HitPointBar(building.Id, MapPoint.CentreOf(building), OverBuilding: true, Fill(building.HitPoints, building.MaxHitPoints)));
            }
        }

        // Units and buildings share one ID sequence, so the two lists interleave.
        bars.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));

        return bars;
    }

    private static double Fill(int hitPoints, int maxHitPoints) => (double)hitPoints / maxHitPoints;
}
