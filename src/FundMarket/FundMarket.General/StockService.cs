using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Helper.Models;
using FundMarket.Mapper;
using FundMarket.Reader.Logic;

namespace FundMarket.Helper;

public class StockService(UnitOfWork unitOfWork, TextWriter writer) : GeneralStockService
{
    /// <summary>
    /// Adds one or multiple ticker symbols to the system. Each ticker is validated using an external data source.
    /// Duplicate tickers will be skipped.
    /// </summary>
    /// <param name="input">
    /// A string containing one or more ticker symbols separated by semicolons (e.g., "AAPL;TSLA;MSFT").
    /// </param>
    /// <param name="reader">Asset Date reader</param>
    public async Task AddTickersAsync(string? input, IAssetReader reader)
    {
        const char delimiter = ';';
        if (input != null)
        {
            List<string> tickers = [];
            if (input.Contains(delimiter))
            {
                tickers.AddRange(input.Split(delimiter));
            }
            else
            {
                tickers.Add(input);
            }
            foreach (var raw in tickers)
            {
                await AddTicker(raw, reader);
            }
        }
    }

    /// <summary>
    /// Gets all saved ticker symbols from the database.
    /// </summary>
    public IEnumerable<Ticker> GetAllTickers()
    {
        return unitOfWork.TickerRepository.Get()
            .OrderBy(t => t.IsIgnored)
            .ThenByDescending(t => t.ExpectedPercent)
            .ThenBy(t => t.Name);
    }

    public async Task UpdateExpectedPercent(string? tickerToUpdate, decimal percentToUpdate)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(tickerToUpdate);
            tickerToUpdate = tickerToUpdate.Trim().ToUpper();
            var ticker = unitOfWork.TickerRepository
                .Get(t => t.Name == tickerToUpdate)
                .FirstOrDefault();
            if (ticker != null)
            {
                ticker.ExpectedPercent = percentToUpdate;
                unitOfWork.TickerRepository.Update(ticker);
                await unitOfWork.SaveChangesAsync();
                await writer.WriteLineAsync("Ticker percent updated.");
            }
            else
            {
                await writer.WriteLineAsync("Ticker not found.");
            }
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync($"Error: {e.Message}");
        }
    }
    
    public async Task UpdateIgnoreFlag(string? tickerToUpdate, bool isIgnored)
    {
        ArgumentNullException.ThrowIfNull(tickerToUpdate);
        tickerToUpdate = tickerToUpdate.Trim().ToUpper();
        var ticker = unitOfWork.TickerRepository
            .Get(t => t.Name == tickerToUpdate)
            .FirstOrDefault();
        if (ticker != null)
        {
            ticker.IsIgnored = isIgnored;
            unitOfWork.TickerRepository.Update(ticker);
            await unitOfWork.SaveChangesAsync();
            await writer.WriteLineAsync("Ticker flag updated.");
        }
        else
        {
            await writer.WriteLineAsync("Ticker not found.");
        }
    }

    public async Task DeleteTicker(string? tickerToUpdate)
    {
        ArgumentNullException.ThrowIfNull(tickerToUpdate);
        tickerToUpdate = tickerToUpdate.Trim().ToUpper();
        var ticker = unitOfWork.TickerRepository
            .Get(t => t.Name == tickerToUpdate)
            .FirstOrDefault();
        if (ticker != null)
        {
            unitOfWork.TickerRepository.Delete(ticker);
            await unitOfWork.SaveChangesAsync();
            await writer.WriteLineAsync("Ticker deleted.");
        }
        else
        {
            await writer.WriteLineAsync("Ticker not found.");
        }
    }

    /// <summary>
    /// Records a purchase of an asset by saving the ticker, quantity, price, and date to the database.
    /// </summary>
    /// <param name="ticker">The stock ticker symbol (e.g., "AAPL").</param>
    /// <param name="qty">The quantity of the asset to purchase.</param>
    /// <param name="price">The price per unit of the asset.</param>
    /// <param name="date">The purchase date.</param>
    public async Task BuyAsset(string? ticker, decimal qty, decimal price, DateTime? date)
    {
        if (unitOfWork.TickerRepository.Get(t => t.Name == ticker).Any())
        {
            try
            {
                unitOfWork.PurchaseRepository.Insert(DBModelMapper.MapPurchase(ticker, qty, price, date ?? DateTime.Today));
                await unitOfWork.SaveChangesAsync();
                await writer.WriteLineAsync($"Added {qty} of {ticker}");
            }
            catch (Exception e)
            {
                await writer.WriteLineAsync("Error: " + e.Message);
            }
        }
        else
        {
            await writer.WriteLineAsync("Invalid ticker");
        }
    }
    
    public async Task SellAsset(string? ticker, decimal qty, decimal price, DateTime? date)
    {
        if (unitOfWork.TickerRepository.Get(t => t.Name == ticker).Any())
        {
            try
            {
                unitOfWork.SaleRepository.Insert(DBModelMapper.MapSale(ticker,qty, price, date ?? DateTime.Now));
                await unitOfWork.SaveChangesAsync();
                await writer.WriteLineAsync($"Sold {qty} of {ticker}");
            }
            catch (Exception e)
            {
                await writer.WriteLineAsync("Error: " + e.Message);
            }
        }
        else
        {
            await writer.WriteLineAsync("Invalid ticker");
        }
    }

    /// <summary>
    /// Gets a summary of all purchased assets, including quantity, average price, current price,
    /// and profit or loss (PnL) for each asset. Also prints the total PnL across all assets.
    /// </summary>
    public async Task<AssetsSummary> GetBoughtAssets(IAssetReader reader)
    {
        var result = new AssetsSummary();
        var purchases = unitOfWork.PurchaseRepository.Get().ToList();
        var sales = unitOfWork.SaleRepository.Get().ToList();
        var tickers = unitOfWork.TickerRepository.Get(t => t.IsIgnored != true).ToList();
        UpdateTickersExpectedPercent(tickers);
        if (purchases.Count == 0)
        {
            return result;
        }
        var assetSummaries = await GetAssetSummary(unitOfWork, purchases, sales, reader);
        var items = assetSummaries.ToList();
        var pnl = decimal.Zero;
        var total = decimal.Zero;
        AssetSummaryItem? best = null;
        AssetSummaryItem? worst = null;
        var totalValue = items.Sum(s => s.CurrentValue);
        foreach (var summary in items.OrderByDescending(s => s.Difference))
        {
            summary.ExpectedPercent = GetSummaryExpectedPercent(tickers, summary);
            pnl += summary.Difference ?? decimal.Zero;
            total += summary.CurrentValue ?? decimal.Zero;
            best ??= summary;
            worst ??= summary;
            if (summary.Difference > best.Difference)
            {
                best = summary;
            }
            if (summary.Difference < worst.Difference)
            {
                worst = summary;
            }
            summary.Percent = summary.CurrentValue * 100m / totalValue;
            result.Assets.Add(summary);
        }
        result.TotalValue = total;
        result.TotalPnL = pnl;
        result.BestAsset = best?.Ticker;
        result.WorstAsset = worst?.Ticker;
        return result;
    }

    private static decimal? GetSummaryExpectedPercent(List<Ticker> tickers, AssetSummaryItem summary)
    {
        return tickers
            .FirstOrDefault(t => t.Name.Equals(summary.Ticker, StringComparison.OrdinalIgnoreCase))
            ?.ExpectedPercent;
    }

    /// <summary>
    /// Gets recommended assets to buy based on the specified amount. 
    /// The method parses the input as a decimal amount and applies internal recommendation logic.
    /// </summary>
    /// <param name="input">The investment amount as a decimal (e.g., "1000").</param>
    /// <param name="reader">Asset Data reader</param>
    /// <returns>A task representing the asynchronous recommendation operation.</returns>
    public async Task<IEnumerable<AssetRecommendation>> GetAssetRecommendation(decimal input, IAssetReader reader)
    {
        return await GetAssetRecommendations(unitOfWork, input, reader);
    }

    /// <summary>
    /// Attempts to add a new ticker symbol to the system. 
    /// Retrieves the asset data from Yahoo and saves it if the ticker does not already exist.
    /// </summary>
    /// <param name="raw">The raw ticker input string, which will be trimmed and normalized.</param>
    private async Task AddTicker(string raw, IAssetReader reader)
    {
        var ticker = raw.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return;
        }
        try
        {
            var asset = await reader.GetAssetAsync(ticker);
            if (unitOfWork.TickerRepository.Get(t => t.Name == asset.Ticker).Any())
            {
                await writer.WriteLineAsync($"Ticker '{ticker}' already exists.");
            }
            else
            {
                unitOfWork.TickerRepository.Insert(DBModelMapper.MapTickerRecord(asset));
                await unitOfWork.SaveChangesAsync();
                await writer.WriteLineAsync($"Ticker '{ticker}' added.");
            }
        }
        catch (Exception)
        {
            await writer.WriteLineAsync($"No price found for ticker '{ticker}'.");
        }
    }
}