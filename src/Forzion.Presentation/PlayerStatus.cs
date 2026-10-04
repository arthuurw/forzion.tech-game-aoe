using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// What the top bar of the HUD shows about a Player: its Resources, its population against its
/// population limit, its Faction and Age, and the Age Advance underway. Names are keys of the
/// game's translations, as the Faction stores them.
/// </summary>
/// <param name="Resources">How much of each Resource the Player has, one entry per Resource in <see cref="ResourceKind"/> order.</param>
/// <param name="Population">Units of the Player, counting those in training queues.</param>
/// <param name="PopulationLimit">How many units the Player may have at once.</param>
/// <param name="FactionNameKey">Key of the text that names the Player's Faction.</param>
/// <param name="AgeNameKey">Key of the text that names, in the Player's Faction, the Age the Player is in.</param>
/// <param name="AgeAdvance">The Age Advance underway, or null while the Player makes none.</param>
public sealed record PlayerStatus(
    IReadOnlyList<ResourceAmount> Resources,
    int Population,
    int PopulationLimit,
    string FactionNameKey,
    string AgeNameKey,
    AgeAdvanceProgress? AgeAdvance)
{
    /// <summary>The status of <paramref name="player"/> in the current tick of the match.</summary>
    /// <exception cref="ArgumentException">The match has no such Player.</exception>
    public static PlayerStatus Of(MatchState state, PlayerId player)
    {
        ArgumentNullException.ThrowIfNull(state);

        var playerState = PlayerLookup.Find(state, player);
        var faction = playerState.Faction;

        return new PlayerStatus(
            Enum.GetValues<ResourceKind>().Select(kind => new ResourceAmount(kind, playerState.AmountOf(kind))).ToList(),
            state.PopulationOf(player),
            state.PopulationLimitOf(player),
            faction.NameKey,
            playerState.CurrentAge.NameKey,
            AgeAdvanceProgress.Of(state, playerState));
    }
}

/// <summary>How much of one Resource a Player has.</summary>
/// <param name="Kind">The Resource.</param>
/// <param name="Amount">How much of it the Player has.</param>
public sealed record ResourceAmount(ResourceKind Kind, int Amount);

/// <summary>An Age Advance underway: the Age it leads to and how far it has gone.</summary>
/// <param name="AgeNameKey">Key of the text that names the Age the advance leads to.</param>
/// <param name="Progress">How far the advance has gone, from 0 (just ordered) towards 1 (done).</param>
public sealed record AgeAdvanceProgress(string AgeNameKey, double Progress)
{
    /// <summary>The Age Advance the Player is making at one of its buildings, or null when it makes none.</summary>
    internal static AgeAdvanceProgress? Of(MatchState state, PlayerState player)
    {
        var advancing = state.Buildings.FirstOrDefault(building =>
            building.Owner == player.Id && building.AgeAdvanceProgress is not null);

        return advancing is null ? null : Of(advancing, player);
    }

    /// <summary>The Age Advance the building is making for its Player, or null when it makes none.</summary>
    internal static AgeAdvanceProgress? Of(BuildingState building, PlayerState player)
    {
        if (building.AgeAdvanceProgress is not { } ticks || player.NextAge is not { } next)
        {
            return null;
        }

        return new AgeAdvanceProgress(next.NameKey, Fractions.Of(ticks, next.AdvanceTime));
    }
}
