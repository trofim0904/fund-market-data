using FundMarket.Core.Extension;
using FundMarket.Core.Models;
using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Reader.Logic;
using FundMarket.Reader.Model;

namespace FundMarket.Core;

public class StockDataRecommendationService(IUnitOfWork unitOfWork, IAssetReader reader) : IStockDataRecommendationService
{
    public async Task<IEnumerable<AssetRecommendation>> GetAssetRecommendationAsync(decimal amountToInvest)
    {
        var tickers = unitOfWork.TickerRepository.Get(t => t.IsIgnored != true).GetWithExpectedPercent();
        var tickerNames = tickers.Select(t => t.Name);
        var purchases = unitOfWork.PurchaseRepository.Get(p => tickerNames.Contains(p.Ticker)).ToList();
        var sales = unitOfWork.SaleRepository.Get(p => tickerNames.Contains(p.Ticker)).ToList();
        var assets = await reader.GetAssetsAsync(tickers.Select(t => t.Name));
        var unpurchased = GetUnpurchasedTickers(tickers, purchases);
        return GetAssetRecommendationAsync(amountToInvest, purchases, sales, tickers, assets.ToList(), unpurchased);
    }

    private IEnumerable<AssetRecommendation> GetAssetRecommendationAsync(decimal amountToInvest,
        List<AssetPurchase> purchases, List<AssetSale> sales, List<Ticker> tickers, List<Asset> assets,
        List<string> unpurchasedTickers)
    {
        var result = new List<AssetRecommendation>();
        var summary = new List<AssetSummaryItem>();
        // Initial recommendation: one share of each unpurchased ticker (if affordable)
        foreach (var ticker in unpurchasedTickers)
        {
            var asset = assets.FirstOrDefault(a => a.Ticker == ticker);
            if (asset != null && amountToInvest >= asset.CurrentPrice)
            {
                AddNewRecommendation(result, asset);
                AddNewItemToSummary(summary, asset);
                amountToInvest -= asset.CurrentPrice;
            }
        }
        // Add existing purchase summaries
        var assetSummaries = Utilities.CreateAssetSummary(purchases, sales, tickers, assets.ToList());
        summary.AddRange(assetSummaries);
        return GetAssetRecommendationAsync(amountToInvest, summary, result);
    }

    private static IEnumerable<AssetRecommendation> GetAssetRecommendationAsync(decimal amountToInvest,
        List<AssetSummaryItem> summary, List<AssetRecommendation> result)
    {
        // Iteratively recommend assets until no more valid recommendations can be made
        bool recommendationMade;
        do
        {
            Utilities.SetAssetSummaryItemsPercent(summary);
            // Filter out assets where current percent >= expected percent
            var underweightAssets = GetUnderweightAssets(summary);
            recommendationMade = false;
            foreach (var asset in underweightAssets.Where(asset => amountToInvest >= asset.CurrentPrice))
            {
                AddRecommendation(result, asset, asset.ExpectedPercent);
                AddSummaryQty(asset);
                amountToInvest -= asset.CurrentPrice ?? decimal.Zero;
                recommendationMade = true;
                break; // Only one recommendation per loop
            }
        } while (recommendationMade);
        return result;
    }

    /// <summary>
    /// Returns ticker names that have not yet been purchased.
    /// </summary>
    private static List<string> GetUnpurchasedTickers(
        IReadOnlyCollection<Ticker> tickers,
        IReadOnlyCollection<AssetPurchase> purchases)
    {
        var purchasedTickers = purchases
            .Select(p => p.Ticker)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tickers
            .Select(t => t.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !purchasedTickers.Contains(name))
            .ToList();
    }

    private static List<AssetSummaryItem> GetUnderweightAssets(List<AssetSummaryItem> summary)
    {
        return summary
            .Where(s => s.Percent < s.ExpectedPercent)
            .OrderByDescending(s => s.ExpectedPercent - s.Percent)
            .ThenBy(s => s.Difference)
            .ToList();
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

    /// <summary>
    /// Increments the quantity for the asset summary.
    /// </summary>
    /// <param name="assetSummary">The asset summary to update.</param>
    private static void AddSummaryQty(AssetSummaryItem assetSummary)
    {
        assetSummary.Qty++;
    }

    private static void AddNewRecommendation(List<AssetRecommendation> result, Asset asset)
    {
        result.Add(new AssetRecommendation(asset.Ticker, Constants.DefaultReason)
        {
            NewAsset = true
        });
    }
    
    /// <summary>
    /// Adds or updates a recommendation entry for the given asset summary.
    /// </summary>
    private static void AddRecommendation(List<AssetRecommendation> result, AssetSummaryItem assetSummary, decimal? expectedPercent)
    {
        if (assetSummary.Ticker == null)
        {
            return;
        }
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