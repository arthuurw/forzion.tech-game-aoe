namespace Forzion.Presentation;

/// <summary>
/// Shares of a whole counted in whole numbers, such as how far some work counted in ticks has
/// gone or how many hit points are left, as the HUD and the map show them.
/// </summary>
public static class Fractions
{
    /// <summary>The share of <paramref name="total"/> that <paramref name="done"/> makes, from 0 to 1; 1 when there is nothing to do.</summary>
    public static double Of(int done, int total) => total <= 0 ? 1 : Math.Clamp((double)done / total, 0, 1);
}
