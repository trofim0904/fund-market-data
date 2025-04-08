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
}
