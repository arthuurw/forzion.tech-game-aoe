namespace Forzion.Simulation;

/// <summary>How a command finds the units it names among those of its issuing Player.</summary>
internal static class OwnUnits
{
    /// <summary>
    /// The units with the given IDs, in the same order, when every one is in the match and
    /// belongs to <paramref name="issuer"/>. Otherwise rejects <paramref name="command"/> with
    /// the reason of the first unit that fails and returns null.
    /// </summary>
    public static List<UnitState>? Find(TickContext context, Command command, PlayerState issuer, IReadOnlyList<EntityId> ids)
    {
        var units = new List<UnitState>(ids.Count);

        foreach (var id in ids)
        {
            var unit = context.State.FindUnit(id);

            if (unit is null)
            {
                context.Reject(command, RejectionReason.UnknownUnit);

                return null;
            }

            if (unit.Owner != issuer.Id)
            {
                context.Reject(command, RejectionReason.UnitOfAnotherPlayer);

                return null;
            }

            units.Add(unit);
        }

        return units;
    }
}
