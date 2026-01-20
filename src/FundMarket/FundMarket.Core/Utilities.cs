using FundMarket.Core.Models;
using FundMarket.Database.Models;
using FundMarket.Reader.Model;

namespace FundMarket.Core;

public static class Utilities
{
    /// <summary>
    /// Builds a portfolio-level asset summary by aggregating purchases and sales,
    /// enriching data with ticker configuration and current market prices.
    /// </summary>
    public static IEnumerable<AssetSummaryItem> CreateAssetSummary(List<AssetPurchase> purchases, List<AssetSale> sales,
        List<Ticker> tickers, List<Asset> assets)
    {
        // Build fast lookup dictionaries to avoid repeated LINQ scans (O(n²))
        var salesByTicker = sales.GroupBy(s => s.Ticker).ToDictionary(g => g.Key!, g => g.ToList());
        var tickerByName = tickers.ToDictionary(t => t.Name, t => t);
        var assetByTicker = assets.ToDictionary(a => a.Ticker, a => a);
        // First pass: build summaries without Percent
        var summaries = purchases
            .GroupBy(p => p.Ticker)
            .Select(g =>
            {
                var ticker = g.Key;
                var totalPurchasedQty = g.Sum(p => p.Qty);
                var soldQty = salesByTicker.TryGetValue(ticker!, out var tickerSales) 
                    ? tickerSales.Sum(s => s.Qty) : decimal.Zero;
                var netQty = totalPurchasedQty - soldQty;
                var purchaseTotal = g.Sum(p => p.Qty * p.Price) - (tickerSales?.Sum(s => s.Qty * s.Price) ?? decimal.Zero);
                var currentPrice = assetByTicker.TryGetValue(ticker!, out var asset) ? asset.CurrentPrice : decimal.Zero;
                return new AssetSummaryItem
                {
                    Ticker = ticker,
                    ExpectedPercent = tickerByName.TryGetValue(ticker!, out var tickerCfg)
                        ? tickerCfg.ExpectedPercent : null,
                    Qty = netQty,
                    AvgPrice = GetAvgPrice(g.ToList()),
                    PurchaseTotal = purchaseTotal,
                    CurrentPrice = currentPrice,
                    // Percent calculated in second pass
                    Percent = 0m
                };
            })
            .Where(s => s.Qty > decimal.Zero)
            .ToList();
        SetAssetSummaryItemsPercent(summaries);
        return summaries;
    }

    public static void SetAssetSummaryItemsPercent(List<AssetSummaryItem> summaries)
    {
        var totalMarketValue = summaries.Sum(s => s.Qty * s.CurrentPrice);
        if (!(totalMarketValue > decimal.Zero))
        {
           return;
        }
        foreach (var summary in summaries)
        {
            summary.Percent = decimal.Round(
                (summary.Qty!.Value * summary.CurrentPrice!.Value) / totalMarketValue.Value * 100m,
                2,
                MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Calculates the weighted average purchase price for a list of asset purchases.
    /// </summary>
    /// <param name="list">The list of purchases to calculate the average price from.</param>
    /// <returns>The weighted average price, rounded to 3 decimal places.</returns>
    public static decimal GetAvgPrice(List<AssetPurchase> list)
    {
        var totalQty = decimal.Zero;
        var totalCost = decimal.Zero;
        foreach (var purchase in list)
        {
            totalQty += purchase.Qty;
            totalCost += purchase.Qty * purchase.Price;
        }
        return totalQty == 0 ? 0 : decimal.Round(totalCost / totalQty, 3);
    }
}