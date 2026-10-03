namespace Forzion.Simulation;

/// <summary>
/// Makes every attacking unit hit its target: a unit with the target within range spends the
/// ticks of its attack interval on it, and the hit lands at the end of them. Ranged units hit
/// from where they stand; nothing flies between the two (no simulated projectile).
/// </summary>
internal sealed class CombatSystem : ISystem
{
    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var unit in state.Units)
        {
            if (unit.Target is not { } targetId || Balance.Attack(unit.Kind) is not { } attack)
            {
                continue;
            }

            var targetUnit = state.FindUnit(targetId);
            var targetBuilding = targetUnit is null ? state.FindBuilding(targetId) : null;
            var distance = targetUnit is not null
                ? Distance(unit.Position, targetUnit.Position)
                : Distance(unit.Position, targetBuilding!);

            if (distance > attack.Range)
            {
                continue;
            }

            unit.AttackProgress++;

            if (unit.AttackProgress < attack.IntervalTicks)
            {
                continue;
            }

            unit.AttackProgress = 0;

            if (targetUnit is not null)
            {
                targetUnit.HitPoints -= attack.Damage;
            }
            else
            {
                targetBuilding!.HitPoints -= attack.Damage;
            }
        }
    }

    private static Fix64 Distance(MapPosition from, MapPosition to) => Fix64.Hypot(to.X - from.X, to.Y - from.Y);

    /// <summary>Distance to the nearest point of the building's footprint; zero inside it.</summary>
    private static Fix64 Distance(MapPosition from, BuildingState building)
    {
        var left = Fix64.FromInt(building.Origin.X);
        var bottom = Fix64.FromInt(building.Origin.Y);
        var right = Fix64.FromInt(building.Origin.X + building.Width);
        var top = Fix64.FromInt(building.Origin.Y + building.Height);
        var x = Fix64.Max(Fix64.Max(left - from.X, from.X - right), Fix64.Zero);
        var y = Fix64.Max(Fix64.Max(bottom - from.Y, from.Y - top), Fix64.Zero);

        return Fix64.Hypot(x, y);
    }
}
