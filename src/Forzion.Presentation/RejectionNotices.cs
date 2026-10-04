using System.Text;
using Forzion.Simulation;

namespace Forzion.Presentation;

/// <summary>
/// Which notices to show the person at the screen when the match refuses their orders. A
/// notice is the key of a text in the game's translations, so the words live outside the
/// code and can be translated.
/// </summary>
public static class RejectionNotices
{
    private const string KeyPrefix = "ORDER_REJECTED_";

    /// <summary>
    /// The message keys for the orders of <paramref name="player"/> that <paramref name="events"/>
    /// report as refused, in the order they were refused. Other Players' refusals are not the
    /// person's business: the AI's refused orders stay off the screen.
    /// </summary>
    public static IEnumerable<string> MessageKeysFor(IEnumerable<MatchEvent> events, PlayerId player)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .OfType<CommandRejected>()
            .Where(rejection => rejection.Command.Player == player)
            .Select(rejection => MessageKeyOf(rejection.Reason));
    }

    /// <summary>
    /// The key of the text explaining <paramref name="reason"/>: <c>ORDER_REJECTED_</c> followed
    /// by the reason's name in upper snake case, such as <c>ORDER_REJECTED_DESTINATION_OUTSIDE_MAP</c>.
    /// Derived from the name so a reason added to the simulation has a key at once; its text
    /// still has to be added to the translations.
    /// </summary>
    public static string MessageKeyOf(RejectionReason reason)
    {
        var name = reason.ToString();
        var key = new StringBuilder(KeyPrefix);

        for (var index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]))
            {
                key.Append('_');
            }

            key.Append(char.ToUpperInvariant(name[index]));
        }

        return key.ToString();
    }
}
