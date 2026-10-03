namespace Forzion.Simulation;

/// <summary>
/// Makes every attacking unit hit its target: a unit with the target within range spends the
/// ticks of its attack interval on it, and the hit lands at the end of them. Ranged units hit
/// from where they stand; nothing flies between the two (no simulated projectile).
/// </summary>
/// <remarks>
/// Hits are simultaneous: a unit struck down earlier in the tick still lands its own hit, so
/// acting first in ID order is no advantage. Whatever is left without hit points is removed at
/// the end of the tick's combat, in the same tick, and the units attacking it stop.
/// </remarks>
internal sealed class CombatSystem : ISystem
{
    public void Run(TickContext context)
    {
        var state = context.State;

        foreach (var unit in state.Units)
        {
            if (Balance.Attack(unit.Kind) is not { } attack)
            {
                continue;
            }

            if (unit.Target is null && !unit.IsMoving && NearestEnemyUnit(state, unit, attack.PerceptionRadius) is { } enemy)
            {
                unit.Attack(enemy.Id);
            }

            if (unit.Target is { } target)
            {
                Fight(state, unit, target, attack);
            }
        }

        var destroyed = state.RemoveDestroyed();

        foreach (var id in destroyed)
        {
            context.Emit(new EntityDestroyed(id));
        }

        foreach (var unit in state.Units)
        {
            if (unit.Target is { } target && destroyed.Contains(target))
            {
                unit.StopAttacking();
            }
        }
    }

    /// <summary>
    /// The unit of another Player nearest to <paramref name="unit"/> within the radius; between
    /// units equally near, the one with the lowest ID. Null when there is none. Buildings do not
    /// draw an idle unit's attack.
    /// </summary>
    private static UnitState? NearestEnemyUnit(MatchState state, UnitState unit, Fix64 radius)
    {
        UnitState? nearest = null;
        var nearestDistance = radius;

        // Ascending ID order and a strict comparison keep the lowest ID among the equally near.
        foreach (var other in state.Units)
        {
            if (other.Owner == unit.Owner)
            {
                continue;
            }

            var distance = Distance(unit.Position, other.Position);

            if (distance < nearestDistance || (nearest is null && distance == nearestDistance))
            {
                nearest = other;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static void Fight(MatchState state, UnitState unit, EntityId target, AttackStats attack)
    {
        var targetUnit = state.FindUnit(target);
        var targetBuilding = targetUnit is null ? state.FindBuilding(target) : null;
        var distance = targetUnit is not null
            ? Distance(unit.Position, targetUnit.Position)
            : Distance(unit.Position, targetBuilding!);

        if (distance > attack.Range)
        {
            unit.AttackProgress = 0;

            if (targetUnit is not null)
            {
                Chase(state.Map, unit, targetUnit.Position.Cell);
            }
            else if (!unit.IsMoving)
            {
                // A building stays put, so the way to it is only searched again when the unit stopped short.
                MovementSystem.WalkTo(state.Map, unit, NearestCellOf(targetBuilding!, unit.Position.Cell));
            }

            return;
        }

        if (unit.IsMoving)
        {
            unit.Stop();
        }

        unit.AttackProgress++;

        if (unit.AttackProgress < attack.IntervalTicks)
        {
            return;
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

    /// <summary>
    /// Keeps the unit walking towards the Cell its target unit stands on. A path already
    /// heading there is kept, so the way is only searched again when the target has moved to
    /// another Cell or the unit has stopped short of it.
    /// </summary>
    private static void Chase(MapState map, UnitState unit, CellPosition destination)
    {
        if (!unit.IsMoving || unit.Path[^1] != destination)
        {
            MovementSystem.WalkTo(map, unit, destination);
        }
    }

    /// <summary>The Cell of the building's footprint nearest to <paramref name="cell"/>.</summary>
    private static CellPosition NearestCellOf(BuildingState building, CellPosition cell) =>
        new(
            Math.Clamp(cell.X, building.Origin.X, building.Origin.X + building.Width - 1),
            Math.Clamp(cell.Y, building.Origin.Y, building.Origin.Y + building.Height - 1));

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
