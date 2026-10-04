namespace Forzion.Presentation.Tests;

public class FractionsTests
{
    [Fact]
    public void A_share_is_the_part_done_over_the_whole()
    {
        Assert.Equal(0.25, Fractions.Of(150, 600));
    }

    [Fact]
    public void A_share_stays_between_0_and_1()
    {
        Assert.Equal(0, Fractions.Of(-5, 10));
        Assert.Equal(1, Fractions.Of(15, 10));
    }

    [Fact]
    public void Work_of_nothing_is_done()
    {
        Assert.Equal(1, Fractions.Of(0, 0));
    }
}
