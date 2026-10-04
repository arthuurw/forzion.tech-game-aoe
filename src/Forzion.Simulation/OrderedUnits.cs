namespace Forzion.Simulation;

/// <summary>The check shared by every command that gives an order to a list of units.</summary>
internal static class OrderedUnits
{
    /// <summary>
    /// The units <paramref name="ids"/> names, in the order named, or null once
    /// <paramref name="command"/> has been rejected. A unit that does not exist, because it
    /// died after the order was given, is skipped. The command is rejected with
    /// <see cref="RejectionReason.UnitOfAnotherPlayer"/> when any of the units belongs to
    /// another Player, and with <see cref="RejectionReason.UnknownUnit"/> when none of them
    /// exists.
    /// </summary>
    public static List<UnitState>? Find(
        TickContext context, Command command, PlayerState issuer, IReadOnlyList<EntityId> ids)
    {
        var units = new List<UnitState>(ids.Count);

        foreach (var id in ids)
        {
            var unit = context.State.FindUnit(id);

            if (unit is null)
            {
                continue;
            }

            if (unit.Owner != issuer.Id)
            {
                context.Reject(command, RejectionReason.UnitOfAnotherPlayer);

                return null;
            }

            units.Add(unit);
        }

        if (units.Count == 0)
        {
            context.Reject(command, RejectionReason.UnknownUnit);

            return null;
        }

        return units;
    }

    /// <summary>
    /// The units <paramref name="ids"/> names that can do the job, in the order named, or null
    /// once <paramref name="command"/> has been rejected. Units are found as by
    /// <see cref="Find(TickContext, Command, PlayerState, IReadOnlyList{EntityId})"/>; a unit
    /// that cannot do the job is then skipped and left to whatever it was doing, so a mixed
    /// group sends those that can. The command is rejected with <paramref name="cannotDoTheJob"/>
    /// when none of the units can.
    /// </summary>
    public static List<UnitState>? Find(
        TickContext context,
        Command command,
        PlayerState issuer,
        IReadOnlyList<EntityId> ids,
        Func<UnitState, bool> canDoTheJob,
        RejectionReason cannotDoTheJob)
    {
        var units = Find(context, command, issuer, ids);

        if (units is null)
        {
            return null;
        }

        var able = units.Where(canDoTheJob).ToList();

        if (able.Count == 0)
        {
            context.Reject(command, cannotDoTheJob);

            return null;
        }

        return able;
    }
}
