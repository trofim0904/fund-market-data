using System.Linq.Expressions;
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
    /// <param name="reader">The stock data reader.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an array
    /// of <see cref="Asset"/> objects with the latest stock data.
    /// </returns>
    protected async Task<Asset[]> LoadGeneralStockData(UnitOfWork unitOfWork, IAssetReader reader)
    {
        return (await reader.GetAssetsAsync(unitOfWork.TickerRepository.Get()
            .Where(t => !string.IsNullOrWhiteSpace(t.Name)).Select(t => t.Name))).ToArray();
    }

    /// <summary>
    /// Loads general stock data by retrieving assets based on predicate.
    /// Uses an <see cref="IAssetReader"/> to fetch data asynchronously.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access the ticker repository.</param>
    /// <param name="reader">The stock data reader.</param>
    /// <param name="predicate">Ticker filter.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an array
    /// of <see cref="Asset"/> objects with the latest stock data.
    /// </returns>
    protected async Task<Asset[]> LoadGeneralStockData(UnitOfWork unitOfWork, IAssetReader reader,
        Expression<Func<Ticker, bool>> predicate)
    {
        return (await reader.GetAssetsAsync(unitOfWork.TickerRepository.Get(predicate)
            .Where(t => !string.IsNullOrWhiteSpace(t.Name)).Select(t => t.Name))).ToArray();
    }

    /// <summary>
    /// Generates a summarized view of the user's asset portfolio including quantity, average price,
    /// total investment, and current market price for each ticker.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to load current stock data.</param>
    /// <param name="purchases">A list of asset purchases grouped by ticker.</param>
    /// <param name="sales"></param>
    /// <param name="reader">The stock data reader.</param>
    /// <returns>
    /// A task that returns a collection of <see cref="AssetSummaryItem"/> representing the current portfolio snapshot.
    /// </returns>
    protected async Task<IEnumerable<AssetSummaryItem>> GetAssetSummary(UnitOfWork unitOfWork, List<AssetPurchase> purchases,
        List<AssetSale> sales, IAssetReader reader)
    {
        var stockData = await LoadGeneralStockData(unitOfWork, reader);
        return GetAssetSummary(purchases, sales, stockData.ToList());
    }

    /// <summary>
    /// Generates a summarized view of the user's asset portfolio including quantity, average price,
    /// total investment, and current market price for each ticker.
    /// </summary>
    /// <param name="assets">A list of asset purchases grouped by ticker.</param>
    /// <param name="purchases"></param>
    /// <param name="stockData">A list of stock data statistic.</param>
    /// <returns>
    /// Collection of <see cref="AssetSummaryItem"/> representing the current portfolio snapshot.
    /// </returns>
    protected IEnumerable<AssetSummaryItem> GetAssetSummary(List<AssetPurchase> purchases, List<AssetSale> sales,
        List<Asset> stockData)
    {
        var assetSummaries = purchases
            .GroupBy(a => a.Ticker)
            .Select(g => new AssetSummaryItem
            {
                Ticker = g.Key,
                Qty = g.Sum(a => a.Qty) - sales.Where(s => s.Ticker == g.Key).Sum(s => s.Qty),
                AvgPrice = GetAvgPrice(g.ToList()),
                PurchaseTotal = g.Sum(a => a.Qty * a.Price) - sales.Where(s => s.Ticker == g.Key).Sum(s => s.Qty * s.Price),
                CurrentPrice = stockData.FirstOrDefault(d => d.Ticker == g.Key)?.CurrentPrice ?? decimal.Zero,
            })
            .Where(s => s.Qty > decimal.Zero);
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
    protected async Task<IEnumerable<AssetRecommendation>> GetAssetRecommendations(UnitOfWork unitOfWork, decimal amt,
        IAssetReader reader)
    {
        const int tiers = 5;
        var result = new List<AssetRecommendation>();
        var summary = new List<AssetSummaryItem>();
        var allTickers = GetTickers(unitOfWork);
        var tickersToIgnore = allTickers.Where(t => t.IsIgnored == true).Select(t => t.Name).ToHashSet();
        var tickers = allTickers.Where(t => t.IsIgnored != true).ToList();
        var purchases = GetPurchases(unitOfWork, tickersToIgnore);
        var sales = GetSales(unitOfWork, tickersToIgnore);
        var stockData = (await LoadGeneralStockData(unitOfWork, reader, ticker => ticker.IsIgnored != true)).ToList();
        var notBoughtTickers = GetNotBoughtTickers(tickers, purchases);
        // Initial recommendation: one share of each not yet bought ticker (if affordable)
        foreach (var ticker in notBoughtTickers)
        {
            var asset = await TryGetStockData(reader, stockData, ticker);
            if (asset != null && amt >= asset.CurrentPrice)
            {
                AddNewRecommendation(result, asset);
                AddNewItemToSummary(summary, asset);
                amt -= asset.CurrentPrice;
            }
        }
        // Add existing purchase summaries
        summary.AddRange(GetAssetSummary(purchases, sales, stockData.ToList()));
        var maxMarketCap = stockData.Max(d => d.MarketCap);
        var minMarketCap = stockData.Min(d => d.MarketCap);
        // Iteratively recommend assets until no more valid recommendations can be made
        bool recommendationMade;
        do
        {
            var totalValue = summary.Sum(s => s.CurrentValue) ?? decimal.Zero;
            // Assign percent weight and tier to each summary item
            foreach (var item in summary)
            {
                var cap = stockData.First(s => s.Ticker == item.Ticker).MarketCap;
                item.Percent = Math.Round((item.CurrentValue ?? decimal.Zero) * 100m / totalValue, 2);
                if (tickers.FirstOrDefault(t => t.Name == item.Ticker) is { ExpectedPercent: not null })
                {
                    item.Tier = 0;
                }
                else
                {
                    item.Tier = GetTier(tiers, maxMarketCap, minMarketCap, cap);
                }
            }
            // do in new loop as we need tier of all items 
            foreach (var assetSummary in summary)
            {
                assetSummary.ExpectedPercent = GetExpectedPercent(summary, assetSummary, tickers);
            }
            // Filter out assets where current percent >= expected percent for the tier
            var underweightAssets = summary
                .Where(s => s.Percent < s.ExpectedPercent)
                .OrderByDescending(s => s.ExpectedPercent - s.Percent)
                .ThenBy(s => s.Difference)
                .ToList();
            recommendationMade = false;
            foreach (var asset in underweightAssets)
            {
                if (amt >= asset.CurrentPrice)
                {
                    AddRecommendation(result, asset, asset.ExpectedPercent);
                    AddSummaryQty(asset);
                    amt -= asset.CurrentPrice ?? decimal.Zero;
                    recommendationMade = true;
                    break; // Only one recommendation per loop
                }
            }
        } while (recommendationMade);
        return result;
    }

    private static List<AssetSale> GetSales(UnitOfWork unitOfWork, HashSet<string> tickersToIgnore)
    {
        return unitOfWork.SaleRepository.Get(s => s.Ticker != null && !tickersToIgnore.Contains(s.Ticker)).ToList();
    }

    private static List<AssetPurchase> GetPurchases(UnitOfWork unitOfWork, HashSet<string> tickersToIgnore)
    {
        return unitOfWork.PurchaseRepository.Get(p => p.Ticker != null && !tickersToIgnore.Contains(p.Ticker)).ToList();
    }

    private static List<Ticker> GetTickers(UnitOfWork unitOfWork)
    {
        return unitOfWork.TickerRepository.Get().ToList();
    }

    private static void AddNewItemToSummary(List<AssetSummaryItem> summary, Asset asset)
    {
        summary.Add(new AssetSummaryItem
        {
            Ticker = asset.Ticker,
            CurrentPrice = asset.CurrentPrice,
            AvgPrice = asset.CurrentPrice,
            Qty = 1,
            PurchaseTotal = asset.CurrentPrice
        });
    }

    private static void AddNewRecommendation(List<AssetRecommendation> result, Asset asset)
    {
        result.Add(new AssetRecommendation(asset.Ticker, Constants.DefaultReason)
        {
            NewAsset = true
        });
    }

    /// <summary>
    /// Determine tickers not yet bought
    /// </summary>
    /// <param name="tickers"></param>
    /// <param name="purchases"></param>
    /// <returns></returns>
    private static List<string?> GetNotBoughtTickers(List<Ticker> tickers, List<AssetPurchase> purchases)
    {
        return tickers
            .Select(t => t.Name)
            .Except(purchases
                .DistinctBy(p => p.Ticker)
                .Select(p => p.Ticker))
            .ToList();
    }

    private async Task<Asset?> TryGetStockData(IAssetReader reader, List<Asset> stockData, string? ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return null;
        }
        var existing = stockData.FirstOrDefault(h => h.Ticker == ticker);
        if (existing != null)
        {
            return existing;
        }
        var asset = await reader.GetAssetAsync(ticker);
        stockData.Add(asset);
        return asset;
    }

    /// <summary>
    /// Calculates the expected percentage allocation for the current asset's tier
    /// based on the ratio of its tier value to the total sum of all tier values in the summary list.
    /// Assumes higher tiers imply a greater expected share.
    /// </summary>
    /// <param name="summary">The list of all asset summaries.</param>
    /// <param name="current">The current asset summary whose tier allocation is being evaluated.</param>
    /// <param name="tickers">The tickets that might have expected percent.</param>
    /// <returns>
    /// A decimal representing the percentage share this tier is expected to hold in the overall distribution.
    /// </returns>
    protected static decimal? GetExpectedPercent(List<AssetSummaryItem> summary, AssetSummaryItem current,
        List<Ticker> tickers)
    {
        if (tickers.FirstOrDefault(t => t.Name == current.Ticker) is { ExpectedPercent: not null } ticker)
        {
            return ticker.ExpectedPercent;
        }
        var expectedPercentTotal = tickers
            .Where(t => t.ExpectedPercent is not null)
            .Sum(t => t.ExpectedPercent) ?? decimal.Zero;
        var leftPercentTotal = 100 - expectedPercentTotal;
        if (leftPercentTotal > 0)
        {
            int currentTier = current.Tier!.Value;
            int totalTier = summary.Sum(s => s.Tier!.Value);
            decimal result = currentTier * 100m / totalTier;
            result = result * leftPercentTotal / expectedPercentTotal;
            return Math.Round(result, 2);
        }
        return decimal.Zero;
    }

    /// <summary>
    /// Increments the quantity for the asset summary and updates the total purchase amount
    /// using the current price.
    /// </summary>
    /// <param name="assetSummary">The asset summary to update.</param>
    private void AddSummaryQty(AssetSummaryItem assetSummary)
    {
        assetSummary.Qty++;
        assetSummary.PurchaseTotal += assetSummary.CurrentPrice ?? decimal.Zero;
    }

    /// <summary>
    /// Adds or updates a recommendation entry for the given asset summary.
    /// </summary>
    /// <param name="result">The list of current recommendations to update.</param>
    /// <param name="assetSummary">The asset summary to base the recommendation on.</param>
    /// <param name="s"></param>
    private void AddRecommendation(List<AssetRecommendation> result, AssetSummaryItem assetSummary, decimal? expectedPercent)
    {
        if (assetSummary.Ticker != null)
        {
            var existing = result.FirstOrDefault(r => r.Ticker == assetSummary.Ticker);
            if (existing != null)
            {
                var newAsset = existing.NewAsset;
                var reason = newAsset 
                    ? $"{existing.Reason} Expected in portfolio {expectedPercent}%"
                    : existing.Reason;
                result.Remove(existing);
                result.Add(new AssetRecommendation(assetSummary.Ticker, reason, existing.Qty + 1));
            }
            else
            {
                var reason = $"Current percent in portfolio is {assetSummary.Percent}%, expected {expectedPercent}%";
                result.Add(new AssetRecommendation(assetSummary.Ticker, reason));
            }
        }
    }

    /// <summary>
    /// Maps an asset’s <see cref="AssetSummaryItem.MarketCapRatio"/> to a tier in the range 1 -> tierCount.
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
