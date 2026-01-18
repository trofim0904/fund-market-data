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
        var result = new List<AssetRecommendation>();
        var summary = new List<AssetSummaryItem>();
        var allTickers = GetTickers(unitOfWork);
        var tickersToIgnore = allTickers.Where(t => t.IsIgnored == true).Select(t => t.Name).ToHashSet();
        var tickers = allTickers.Where(t => t.IsIgnored != true).ToList();
        var purchases = GetPurchases(unitOfWork, tickersToIgnore);
        var sales = GetSales(unitOfWork, tickersToIgnore);
        var stockData = (await LoadGeneralStockData(unitOfWork, reader, ticker => ticker.IsIgnored != true)).ToList();
        var notBoughtTickers = GetNotBoughtTickers(tickers, purchases);
        UpdateTickersExpectedPercent(tickers);
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
        // Iteratively recommend assets until no more valid recommendations can be made
        bool recommendationMade;
        do
        {
            var totalValue = summary.Sum(s => s.CurrentValue) ?? decimal.Zero;
            // Assign percent weight to each summary item
            foreach (var item in summary)
            {
                item.Percent = Math.Round((item.CurrentValue ?? decimal.Zero) * 100m / totalValue, 2);
                item.ExpectedPercent = GetExpectedPercent(item, tickers);
            }
            // Filter out assets where current percent >= expected percent
            var underweightAssets = GetUnderweightAssets(summary);
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

    private static List<AssetSummaryItem> GetUnderweightAssets(List<AssetSummaryItem> summary)
    {
        return summary
            .Where(s => s.Percent < s.ExpectedPercent)
            .OrderByDescending(s => s.ExpectedPercent - s.Percent)
            .ThenBy(s => s.Difference)
            .ToList();
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
        var tickers = unitOfWork.TickerRepository.Get().ToList();
        return tickers;
    }

    protected static void UpdateTickersExpectedPercent(List<Ticker> tickers)
    {
        var expectedPercentTotal = tickers
            .Where(t => t.ExpectedPercent is not null)
            .Sum(t => t.ExpectedPercent) ?? decimal.Zero;
        var ticketsWithoutPercentCount = tickers.Count(t => t.ExpectedPercent is null or decimal.Zero);
        var leftPercentTotal = 100 - expectedPercentTotal;
        var percentToSet = decimal.Zero;
        if (leftPercentTotal > decimal.Zero)
        {
            decimal result = leftPercentTotal / ticketsWithoutPercentCount;
            percentToSet = Math.Round(result, 2);
        }
        foreach (var ticker in tickers.Where(t => t.ExpectedPercent is null or decimal.Zero))
        {
            ticker.ExpectedPercent = percentToSet;
        }
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
    /// <param name="current">The current asset summary whose tier allocation is being evaluated.</param>
    /// <param name="tickers">The tickets that might have expected percent.</param>
    /// <returns>
    /// A decimal representing the percentage share this tier is expected to hold in the overall distribution.
    /// </returns>
    protected static decimal? GetExpectedPercent(AssetSummaryItem current,
        List<Ticker> tickers)
    {
        return tickers.FirstOrDefault(t => t.Name == current.Ticker)?.ExpectedPercent ?? decimal.Zero;
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
}
