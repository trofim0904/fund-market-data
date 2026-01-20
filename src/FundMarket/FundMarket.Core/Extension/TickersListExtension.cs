using FundMarket.Database.Models;

namespace FundMarket.Core.Extension;

/// <summary>
/// Extensions for working with collections of <see cref="Ticker"/>.
/// </summary>
public static class TickersListExtension
{
    /// <summary>
    /// Ensures that all tickers have an <see cref="Ticker.ExpectedPercent"/> value.
    /// </summary>
    public static List<Ticker> GetWithExpectedPercent(this IEnumerable<Ticker> tickers)
    {
        var list = tickers.ToList();
        var expectedPercentTotal = list
            .Where(t => t.ExpectedPercent is not null)
            .Sum(t => t.ExpectedPercent) ?? decimal.Zero;
        var tickersWithoutPercentCount =
            list.Count(t => t.ExpectedPercent is null or decimal.Zero);
        if (tickersWithoutPercentCount == 0)
        {
            return list;
        }
        var leftPercentTotal = 100m - expectedPercentTotal;
        var percentToSet = decimal.Zero;
        if (leftPercentTotal > decimal.Zero)
        {
            percentToSet = Math.Round(
                leftPercentTotal / tickersWithoutPercentCount,
                2,
                MidpointRounding.AwayFromZero);
        }
        foreach (var ticker in list.Where(t => t.ExpectedPercent is null or decimal.Zero))
        {
            ticker.ExpectedPercent = percentToSet;
        }
        return list;
    }
}