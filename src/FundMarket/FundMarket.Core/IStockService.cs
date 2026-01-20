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

    Task SellAssetAsync(string name, decimal sellOrderQty, decimal sellOrderPrice, DateTime sellOrderDate);

    /// <summary>
    /// Gets all saved ticker symbols from the database.
    /// </summary>
    Task<IEnumerable<Ticker>> GetAllTickersAsync();
}