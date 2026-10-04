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
        var texts = TranslationTable.Load("pt_BR");

        var text = HudTexts.CostText(new Cost(60, 0, 20), key => texts[key]);

        Assert.Equal("60 Alimento, 20 Ouro", text);
    }

    // Another language may put the name before the amount, or join the amounts otherwise.
    [Fact]
    public void A_cost_takes_the_order_of_amount_and_name_and_what_joins_them_from_the_translations()
    {
        var texts = new Dictionary<string, string>
        {
            [HudTexts.CostAmount] = "{1} {0}",
            [HudTexts.CostSeparator] = " + ",
        };

        var text = HudTexts.CostText(new Cost(60, 0, 20), key => texts.GetValueOrDefault(key, $"<{key}>"));

        Assert.Equal("<RESOURCE_FOOD> 60 + <RESOURCE_GOLD> 20", text);
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
        var texts = TranslationTable.Load("pt_BR");

        Assert.Equal("0%", HudTexts.Percent(0, key => texts[key]));
        Assert.Equal("99%", HudTexts.Percent(0.999, key => texts[key]));
        Assert.Equal("100%", HudTexts.Percent(1, key => texts[key]));
    }

    [Fact]
    public void A_text_with_two_arguments_reads_as_the_Portuguese_translation_puts_them()
    {
        var texts = TranslationTable.Load("pt_BR");
        string Translate(string key) => texts[key];

        Assert.Equal("Alimento: 200", HudTexts.Format(Translate, HudTexts.Labelled, Translate("RESOURCE_FOOD"), 200));
        Assert.Equal("4/5", HudTexts.Format(Translate, HudTexts.PopulationOfLimit, 4, 5));
        Assert.Equal("Portugueses · Era das Feitorias", HudTexts.Format(Translate, HudTexts.FactionAndAge, "Portugueses", "Era das Feitorias"));
        Assert.Equal("1200/1500", HudTexts.Format(Translate, HudTexts.HitPointsOfMax, 1200, 1500));
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

    [Fact]
    public void The_Ages_reached_by_a_Player_the_match_does_not_have_are_refused_as_the_status_and_panel_are()
    {
        var state = Match.Create(PlainConfig()).State;
        var stranger = new PlayerId(9);

        Assert.Throws<ArgumentException>(() => HudTexts.AgesReachedBy([], state, stranger));
        Assert.Throws<ArgumentException>(() => PlayerStatus.Of(state, stranger));
        Assert.Throws<ArgumentException>(() => SelectionPanel.For(state, stranger, []));
    }
}
