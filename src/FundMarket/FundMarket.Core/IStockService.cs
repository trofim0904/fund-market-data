using FundMarket.Core.Models;
using FundMarket.Database.Models;

namespace FundMarket.Core;

public interface IStockService
{
    /// <summary>
    /// Adds one symbols to the system. Duplicate tickers will be skipped.
    /// </summary>
    Task AddTickerAsync(string name);

    /// <summary>
    /// Deletes one symbols from the system.
    /// </summary>
    Task DeleteTickerAsync(string name);

    /// <summary>
    /// Updates one symbols in the system with new percent and ignore flag.
    /// </summary>
    Task UpdateTickerAsync(string name, decimal percent, bool isIgnored);

    /// <summary>
    /// Gets a summary of all purchased assets, including quantity, average price, current price,
    /// and profit or loss (PnL) for each asset.
    /// </summary>
    Task<AssetsSummary> GetBoughtAssetsAsync();

    Task BuyAssetAsync(string name, decimal buyOrderQty, decimal buyOrderPrice, DateTime buyOrderDate);

    Task DeleteBuyAssetOrder(Guid id);

    Task UpdateBuyAssetOrder(Guid id, string name, decimal qty, decimal price, DateTime date);

    Task<IEnumerable<AssetPurchase>> GetAllBuyOrders();

    Task SellAssetAsync(string name, decimal sellOrderQty, decimal sellOrderPrice, DateTime sellOrderDate);

    Task DeleteSellAssetOrder(Guid id);

    Task UpdateSellAssetOrder(Guid id, string name, decimal qty, decimal price, DateTime date);

    Task<IEnumerable<AssetSale>> GetAllSellOrders();

    /// <summary>
    /// Gets all saved ticker symbols from the database.
    /// </summary>
    Task<IEnumerable<Ticker>> GetAllTickersAsync();

    Task<decimal> GetCurrentPriceAsync(string name);
}
