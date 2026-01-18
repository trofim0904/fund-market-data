using System.Globalization;
using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Helper.Models;
using FundMarket.Mapper;
using FundMarket.Reader.Logic;
using FundMarket.Reader.Model;

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
    public async Task AddTickersAsync(string? input, IAssetReader reader)
    {
        const char delimiter = ';';
        if (input != null)
        {
            List<string> tickers = new List<string>();
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
        foreach (var ticker in unitOfWork.TickerRepository.Get()
                     .OrderBy(t => t.IsIgnored)
                     .ThenByDescending(t => t.ExpectedPercent)
                     .ThenBy(t => t.Name))
        {
           yield return ticker;
        }
    }

    public async Task UpdateExpectedPercent(string? tickerToUpdate, string? percentToUpdate)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(tickerToUpdate);
            ArgumentNullException.ThrowIfNull(percentToUpdate);
            if (decimal.TryParse(percentToUpdate, NumberStyles.Any, CultureInfo.InvariantCulture, out var newPercent))
            {
                tickerToUpdate = tickerToUpdate.Trim().ToUpper();
                var ticker = unitOfWork.TickerRepository
                    .Get(t => t.Name == tickerToUpdate)
                    .FirstOrDefault();
                if (ticker != null)
                {
                    ticker.ExpectedPercent = newPercent;
                    unitOfWork.TickerRepository.Update(ticker);
                    await unitOfWork.SaveChangesAsync();
                    await writer.WriteLineAsync("Ticker percent updated.");
                }
                else
                {
                    await writer.WriteLineAsync("Ticker not found.");
                }
            }
            else
            {
                await writer.WriteLineAsync("Wrong percent format.");
            }
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync($"Error: {e.Message}");
        }
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

    public async Task UpdateIgnoreFlag(string? tickerToUpdate)
    {
        ArgumentNullException.ThrowIfNull(tickerToUpdate);
        tickerToUpdate = tickerToUpdate.Trim().ToUpper();
        var ticker = unitOfWork.TickerRepository
            .Get(t => t.Name == tickerToUpdate)
            .FirstOrDefault();
        if (ticker != null)
        {
            ticker.IsIgnored = !(ticker.IsIgnored ?? false);
            unitOfWork.TickerRepository.Update(ticker);
            await unitOfWork.SaveChangesAsync();
            await writer.WriteLineAsync("Ticker flag updated.");
        }
        else
        {
            await writer.WriteLineAsync("Ticker not found.");
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
    /// Retrieves today's stock data for all tickers from the database that are not ignored
    /// and outputs each asset.
    /// </summary>
    public async Task SeeCurrentData(IAssetReader reader)
    {
        try 
        {
            var assets = await LoadGeneralStockData(unitOfWork, reader, ticker => ticker.IsIgnored != true);
            foreach (var asset in assets)
            {
                writer.WriteLine(asset);
            }
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync(e.Message);
        }
    }

    /// <summary>
    /// Fetches and displays current stock data for the specified ticker symbol.
    /// </summary>
    /// <param name="ticker">The stock ticker symbol (e.g., "AAPL", "TSLA").</param>
    /// <param name="reader">The stock data reader.</param>
    public async Task SeeTickerData(string? ticker, IAssetReader reader)
    {
        if (ticker != null)
        {
            Asset? asset = null;
            try
            {
                asset = await reader.GetAssetAsync(ticker);
            }
            catch (Exception)
            {
                await writer.WriteLineAsync("No price for ticker");
            }
            if (asset != null)
            {
                writer.WriteLine(asset);
            }
        }
    }

    /// <summary>
    /// Records a purchase of an asset by saving the ticker, quantity, price, and date to the database.
    /// </summary>
    /// <param name="ticker">The stock ticker symbol (e.g., "AAPL").</param>
    /// <param name="qty">The quantity of the asset to purchase.</param>
    /// <param name="price">The price per unit of the asset.</param>
    /// <param name="date">The purchase date in a valid format (e.g., "yyyy-MM-dd").</param>
    public async Task BuyAsset(string? ticker, string? qty, string? price, string? date)
    {
        ticker = ticker?.ToUpper();
        decimal.TryParse(qty, NumberStyles.Any, CultureInfo.InvariantCulture, out var qtyDecimal);
        decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceDecimal);
        if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            dateTime = DateTime.Today;
        }
        await BuyAsset(ticker, qtyDecimal, priceDecimal, dateTime);
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

    public async Task SellAsset(string? ticker, string? qty, string? price, string? date)
    {
        ticker = ticker?.ToUpper();
        decimal.TryParse(qty, NumberStyles.Any, CultureInfo.InvariantCulture, out var qtyDecimal);
        decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceDecimal);
        if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            dateTime = DateTime.Today;
        }
        await SellAsset(ticker, qtyDecimal, priceDecimal, dateTime);
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
        if (purchases.Count != 0)
        {
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
        }
        return result;
    }

    private static decimal? GetSummaryExpectedPercent(List<Ticker> tickers, AssetSummaryItem summary)
    {
        return tickers
            .FirstOrDefault(t => t.Name.Equals(summary.Ticker, StringComparison.OrdinalIgnoreCase))
            ?.ExpectedPercent;
    }

    /// <summary>
    /// Recommends assets to buy based on the specified amount. 
    /// The method parses the input as a decimal amount and applies internal recommendation logic.
    /// </summary>
    /// <param name="input">The investment amount as a string (e.g., "1000").</param>
    /// <returns>A task representing the asynchronous recommendation operation.</returns>
    public async Task RecommendAssetsAsync(string? input, IAssetReader reader)
    {
        try 
        {
            decimal.TryParse(input, out decimal amt);
            var recommendations = await GetAssetRecommendations(unitOfWork, amt, reader);
            foreach (var recommendation in recommendations.OrderByDescending(r => r.NewAsset))
            {
                BuyTicketRecommendation(recommendation);
            }
        }
        catch (Exception e)
        {
            await writer.WriteLineAsync("Error: not able to get assets. " + e.Message);
        }
    }

    /// <summary>
    /// Gets recommended assets to buy based on the specified amount. 
    /// The method parses the input as a decimal amount and applies internal recommendation logic.
    /// </summary>
    /// <param name="input">The investment amount as a string (e.g., "1000").</param>
    /// <returns>A task representing the asynchronous recommendation operation.</returns>
    public async Task<IEnumerable<AssetRecommendation>> GetAssetRecommendation(string? input, IAssetReader reader)
    {
        decimal.TryParse(input, out decimal amt);
        return await GetAssetRecommendations(unitOfWork, amt, reader);
    }

    /// <summary>
    /// Gets recommended assets to buy based on the specified amount. 
    /// The method parses the input as a decimal amount and applies internal recommendation logic.
    /// </summary>
    /// <param name="input">The investment amount as a decimal (e.g., "1000").</param>
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

    /// <summary>
    /// Outputs a recommendation to buy a specific stock (ticker) for the given amount.
    /// </summary>
    /// <param name="recommendation">The recommended object of type <see cref="AssetRecommendation"/></param>
    private void BuyTicketRecommendation(AssetRecommendation recommendation)
    {
        BuyTicketRecommendation(recommendation.Ticker, recommendation.Qty, recommendation.Reason);
    }

    /// <summary>
    /// Outputs a recommendation to buy a specific stock (ticker) for the given amount.
    /// </summary>
    /// <param name="ticker">The stock ticker symbol (e.g., "AAPL", "TSLA").</param>
    /// <param name="qty">The recommended qty to invest.</param>
    /// <param name="recommendationReason"></param>
    private void BuyTicketRecommendation(string? ticker, decimal qty, string recommendationReason)
    {
        writer.WriteLine($"Recommendation. Buy {qty} shares of ticker {ticker}. {recommendationReason}");
    }
}