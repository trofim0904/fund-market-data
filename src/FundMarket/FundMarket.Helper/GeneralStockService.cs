using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Helper.Models;
using FundMarket.Reader.Logic;
using FundMarket.Reader.Model;

namespace FundMarket.Helper;

/// <summary>
/// Base service that provides core functionality for stock data.
/// </summary>
public abstract class GeneralStockService
{
    /// <summary>
    /// Loads general stock data by retrieving assets for all tickers available in the repository.
    /// Uses an <see cref="IAssetReader"/> to fetch data asynchronously.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access the ticker repository.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an array
    /// of <see cref="Asset"/> objects with the latest stock data.
    /// </returns>
    protected async Task<Asset[]> LoadGeneralStockData(UnitOfWork unitOfWork)
    {
        IAssetReader reader = new YahooHtmlPageReader();
        List<Task<Asset>> tasks = [];
        foreach (var ticker in unitOfWork.TickerRepository.Get())
        {
            if (!string.IsNullOrWhiteSpace(ticker.Name))
            {
                tasks.Add(reader.GetAssetAsync(ticker.Name));
            }
        }
        var assets = await Task.WhenAll(tasks);
        return assets;
    }

    /// <summary>
    /// Generates a summarized view of the user's asset portfolio including quantity, average price,
    /// total investment, and current market price for each ticker.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to load current stock data.</param>
    /// <param name="assets">A list of asset purchases grouped by ticker.</param>
    /// <returns>
    /// A task that returns a collection of <see cref="AssetSummary"/> representing the current portfolio snapshot.
    /// </returns>
    protected async Task<IEnumerable<AssetSummary>> GetAssetSummary(UnitOfWork unitOfWork, List<AssetPurchase> assets)
    {
        var stockData = await LoadGeneralStockData(unitOfWork);
        return GetAssetSummary(assets, stockData.ToList());
    }

    /// <summary>
    /// Generates a summarized view of the user's asset portfolio including quantity, average price,
    /// total investment, and current market price for each ticker.
    /// </summary>
    /// <param name="assets">A list of asset purchases grouped by ticker.</param>
    /// <param name="stockData">A list of stock data statistic.</param>
    /// <returns>
    /// Collection of <see cref="AssetSummary"/> representing the current portfolio snapshot.
    /// </returns>
    protected IEnumerable<AssetSummary> GetAssetSummary(List<AssetPurchase> assets, List<Asset> stockData)
    {
        var assetSummaries = assets
            .GroupBy(a => a.Ticker)
            .Select(g => new AssetSummary
            {
                Ticker = g.Key,
                Qty = g.Sum(a => a.Qty),
                AvgPrice = GetAvgPrice(g.ToList()),
                PurchaseTotal = g.Sum(a => a.Qty * a.Price),
                CurrentPrice = stockData.FirstOrDefault(d => d.Ticker == g.Key)?.CurrentPrice ?? decimal.Zero,
            });
        return assetSummaries;
    }

    /// <summary>
    /// Calculates the weighted average purchase price for a list of asset purchases.
    /// </summary>
    /// <param name="list">The list of purchases to calculate the average price from.</param>
    /// <returns>The weighted average price, rounded to 3 decimal places.</returns>
    private decimal GetAvgPrice(List<AssetPurchase> list)
    {
        decimal totalQty = decimal.Zero;
        decimal totalCost = decimal.Zero;
        foreach (var purchase in list)
        {
            totalQty += purchase.Qty;
            totalCost += purchase.Qty * purchase.Price;
        }
        return totalQty == 0 ? 0 : decimal.Round(totalCost / totalQty, 3);
    }

    /// <summary>
    /// Generates a list of recommended assets to purchase based on remaining funds and current portfolio allocation.
    /// The recommendation aims to balance holdings across tiers determined by market cap.
    /// </summary>
    /// <param name="unitOfWork">The unit of work providing access to tickers and purchases.</param>
    /// <param name="amt">The amount of money available to invest.</param>
    /// <returns>A collection of <see cref="AssetRecommendation"/> objects based on portfolio gaps and weight balancing.</returns>
    /// <exception cref="Exception">Thrown if no assets can be recommended with the provided amount.</exception>
    protected async Task<IEnumerable<AssetRecommendation>> GetAssetRecommendations(UnitOfWork unitOfWork, decimal amt)
    {
        const int tiers = 8;
        var result = new List<AssetRecommendation>();
        var summary = new List<AssetSummary>();
        var tickers = unitOfWork.TickerRepository.Get().ToList();
        var purchases = unitOfWork.PurchaseRepository.Get().ToList();
        var stockData = await LoadGeneralStockData(unitOfWork);
        // Determine tickers not yet bought
        var notBoughtTickers = tickers
            .Select(t => t.Name)
            .Except(purchases
                .DistinctBy(p => p.Ticker)
                .Select(p => p.Ticker))
            .ToList();
        // Initial recommendation: one share of each not yet bought ticker (if affordable)
        foreach (var ticker in notBoughtTickers)
        {
            var asset = stockData.FirstOrDefault(h => h.Ticker == ticker);
            if (asset != null && amt >= asset.CurrentPrice)
            {
                result.Add(new AssetRecommendation(asset.Ticker));
                summary.Add(new AssetSummary
                {
                    Ticker = asset.Ticker,
                    CurrentPrice = asset.CurrentPrice,
                    AvgPrice = asset.CurrentPrice,
                    Qty = 1,
                    PurchaseTotal = asset.CurrentPrice
                });
                amt -= asset.CurrentPrice;
            }
        }
        // Add existing purchase summaries
        summary.AddRange(GetAssetSummary(purchases, stockData.ToList()));
        var maxMarketCap = stockData.Max(d => d.MarketCap);
        var minMarketCap = stockData.Min(d => d.MarketCap);
        // Iteratively recommend assets until no more valid recommendations can be made
        bool recommendationMade;
        do
        {
            var totalValue = summary.Sum(s => s.PurchaseTotal);
            // Assign percent weight and tier to each summary item
            foreach (var item in summary)
            {
                var cap = stockData.First(s => s.Ticker == item.Ticker).MarketCap;
                item.Percent = item.PurchaseTotal * 100m / totalValue;
                item.Tier = GetTier(tiers, maxMarketCap, minMarketCap, cap);
            }
            // Filter out assets where current percent >= expected percent for the tier
            var underweightAssets = summary
                .Where(s => s.Percent < GetExpectedPercent(summary, s))
                .OrderBy(s => s.Difference)
                .ToList();
            recommendationMade = false;
            foreach (var asset in underweightAssets)
            {
                if (amt >= asset.CurrentPrice)
                {
                    AddRecommendation(result, asset);
                    AddSummaryQty(asset);
                    amt -= asset.CurrentPrice ?? decimal.Zero;
                    recommendationMade = true;
                    break; // Only one recommendation per loop
                }
            }
        } while (recommendationMade);
        if (result.Count == 0)
        {
            throw new Exception("Increase invest amount");
        }
        return result;
    }

    /// <summary>
    /// Calculates the expected percentage allocation for the current asset's tier
    /// based on the ratio of its tier value to the total sum of all tier values in the summary list.
    /// Assumes higher tiers imply a greater expected share.
    /// </summary>
    /// <param name="summary">The list of all asset summaries.</param>
    /// <param name="current">The current asset summary whose tier allocation is being evaluated.</param>
    /// <returns>
    /// A decimal representing the percentage share this tier is expected to hold in the overall distribution.
    /// </returns>
    private static decimal? GetExpectedPercent(List<AssetSummary> summary, AssetSummary current)
    {
        int currentTier = current.Tier!.Value;
        int totalTier = summary.Sum(s => s.Tier!.Value);
        return currentTier * 100m / totalTier;
    }

    /// <summary>
    /// Increments the quantity for the asset summary and updates the total purchase amount
    /// using the current price.
    /// </summary>
    /// <param name="assetSummary">The asset summary to update.</param>
    private void AddSummaryQty(AssetSummary assetSummary)
    {
        assetSummary.Qty++;
        assetSummary.PurchaseTotal += assetSummary.CurrentPrice ?? decimal.Zero;
    }

    /// <summary>
    /// Adds or updates a recommendation entry for the given asset summary.
    /// </summary>
    /// <param name="result">The list of current recommendations to update.</param>
    /// <param name="assetSummary">The asset summary to base the recommendation on.</param>
    private void AddRecommendation(List<AssetRecommendation> result, AssetSummary assetSummary)
    {
        if (assetSummary.Ticker != null)
        {
            var existing = result.FirstOrDefault(r => r.Ticker == assetSummary.Ticker);
            if (existing != null)
            {
                result.Remove(existing);
                result.Add(new AssetRecommendation(assetSummary.Ticker, existing.Qty + 1));
            }
            else
            {
                result.Add(new AssetRecommendation(assetSummary.Ticker));
            }
        }
    }

    /// <summary>
    /// Maps an asset’s <see cref="AssetSummary.MarketCapRatio"/> to a tier in the range 1 -> tierCount.
    /// A logarithmic scale is used so that the wide is distributed evenly.
    /// Tier 1 corresponds to the smallest ratios, <paramref name="tierCount"/> to the largest.
    /// </summary>
    /// <param name="tierCount">How many tiers you want (must be ≥ 1).</param>
    /// <param name="maxMarketCap">Max Market Cap</param>
    /// <param name="minMarketCap">Min Market Cap</param>
    /// <param name="cap">Current Market Cap</param>
    /// <returns>The tier index, from 1 up to <paramref name="tierCount"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tierCount"/> is less than 1.
    /// </exception>
    private int GetTier(int tierCount, decimal maxMarketCap, decimal minMarketCap, decimal cap)
    {
        if (tierCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tierCount), "Tier count must be at least 1.");
        }
        // Clamp to the expected range to avoid Log10 issues or out‑of‑range results
        decimal minRatio = minMarketCap;
        decimal maxRatio = maxMarketCap;
        decimal ratio = Math.Clamp(cap, minRatio, maxRatio);
        // Convert to double for Math.Log10, then normalise to 0‑1
        double logRatio = Math.Log10((double)ratio);
        double logMin = Math.Log10((double)minRatio);
        double logMax = Math.Log10((double)maxRatio);
        double normalised = (logRatio - logMin) / (logMax - logMin); // 0 → 1
        // Scale to the requested tier count (1‑based)
        int tier = (int)Math.Floor(normalised * tierCount) + 1;
        // Guard against rounding putting us above the maximum
        return tier > tierCount ? tierCount : tier;
    }
}
