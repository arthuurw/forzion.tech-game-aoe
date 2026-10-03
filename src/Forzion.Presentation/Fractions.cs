namespace Forzion.Presentation;

/// <summary>How far some work counted in ticks has gone, as the HUD shows it.</summary>
internal static class Fractions
{
    /// <summary>The share of <paramref name="total"/> that <paramref name="done"/> makes, from 0 to 1; 1 when there is nothing to do.</summary>
    public static double Of(int done, int total) => total <= 0 ? 1 : Math.Clamp((double)done / total, 0, 1);
}
