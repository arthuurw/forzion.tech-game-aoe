using System.Reflection;
using Forzion.Simulation;
using static Forzion.Presentation.Tests.TestMatches;

namespace Forzion.Presentation.Tests;

public class HudTextsTests
{
    // A label added to the HUD without a text fails here instead of showing a bare key on screen.
    [Fact]
    public void Every_label_of_the_HUD_has_a_Portuguese_text_in_the_game_translations()
    {
        var texts = TranslationTable.Load("pt_BR");
        var keys = typeof(HudTexts)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetValue(null)!)
            .ToList();

        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.True(texts.TryGetValue(key, out var text) && text.Length > 0, $"No Portuguese text for {key}."));
    }

    [Fact]
    public void A_cost_reads_as_the_amount_of_each_Resource_it_takes_leaving_out_the_others()
    {
        var text = HudTexts.CostText(new Cost(60, 0, 20), key => $"<{key}>");

        Assert.Equal("60 <RESOURCE_FOOD>, 20 <RESOURCE_GOLD>", text);
    }

    [Fact]
    public void A_cost_of_nothing_reads_as_free()
    {
        var text = HudTexts.CostText(new Cost(0, 0, 0), key => $"<{key}>");

        Assert.Equal($"<{HudTexts.Free}>", text);
    }

    [Fact]
    public void A_percentage_is_rounded_down_so_100_means_done()
    {
        Assert.Equal("0%", HudTexts.Percent(0));
        Assert.Equal("99%", HudTexts.Percent(0.999));
        Assert.Equal("100%", HudTexts.Percent(1));
    }

    [Fact]
    public void Only_the_Ages_the_Player_reaches_become_notices()
    {
        var match = OfThreeAges();
        var driver = new MatchDriver(match, new TickClock(Match.TicksPerSecond));
        match.Enqueue(new AgeAdvanceCommand(FirstPlayer, TownCenterOf(match, FirstPlayer).Id));
        match.Enqueue(new AgeAdvanceCommand(SecondPlayer, TownCenterOf(match, SecondPlayer).Id));

        // Twenty ticks: enough for the 10 of the advance to Age II.
        var events = driver.Advance(0.5).Concat(driver.Advance(0.5)).ToList();

        Assert.Equal(["TEST_AGE_2"], HudTexts.AgesReachedBy(events, match.State, FirstPlayer));
    }
}
