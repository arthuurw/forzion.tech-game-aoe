using Forzion.Simulation;

namespace Forzion.Presentation.Tests;

public class RejectionNoticesTests
{
    private static readonly PlayerId FirstPlayer = new(1);
    private static readonly PlayerId SecondPlayer = new(2);

    [Fact]
    public void Only_the_rejected_orders_of_the_Player_become_notices_in_the_order_they_happened()
    {
        var outsideTheMap = new MoveCommand(FirstPlayer, [new EntityId(1)], new CellPosition(-1, 0));
        var unknownSource = new GatherCommand(FirstPlayer, [new EntityId(1)], new EntityId(999));
        var enemyOrder = new MoveCommand(SecondPlayer, [new EntityId(2)], new CellPosition(-1, 0));
        MatchEvent[] events =
        [
            new CommandRejected(outsideTheMap, RejectionReason.DestinationOutsideMap),
            new ResourceSourceDepleted(new EntityId(3)),
            new CommandRejected(enemyOrder, RejectionReason.DestinationOutsideMap),
            new CommandRejected(unknownSource, RejectionReason.UnknownResourceSource),
        ];

        var notices = RejectionNotices.MessageKeysFor(events, FirstPlayer);

        Assert.Equal(
            [RejectionNotices.MessageKeyOf(RejectionReason.DestinationOutsideMap), RejectionNotices.MessageKeyOf(RejectionReason.UnknownResourceSource)],
            notices);
    }

    [Fact]
    public void Each_reason_has_a_message_of_its_own()
    {
        var reasons = Enum.GetValues<RejectionReason>();

        Assert.Equal(reasons.Length, reasons.Select(RejectionNotices.MessageKeyOf).Distinct().Count());
    }

    // The texts live in the game's translation table, outside the code. A reason added to the
    // simulation without a text there fails here instead of showing a bare key on screen.
    [Fact]
    public void Every_reason_has_a_Portuguese_text_in_the_game_translations()
    {
        var texts = TranslationTable.Load("pt_BR");

        Assert.All(Enum.GetValues<RejectionReason>(), reason =>
        {
            var key = RejectionNotices.MessageKeyOf(reason);

            Assert.True(texts.TryGetValue(key, out var text) && text.Length > 0, $"No Portuguese text for {key}.");
        });
    }
}
