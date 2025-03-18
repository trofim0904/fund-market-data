using System.Globalization;
using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Database.Repository;
using FundMarket.Mapper;
using FundMarket.Reader.Logic;
using FundMarket.Reader.Model;

namespace FundMarket;

public static class StockService
{
    /// <summary>
    /// Loads all tickers data specified in <see cref="Setup.Tickers"/> and saves to the DB.
    /// </summary>
    public static async Task LoadData()
    {
        await using HistoryRecordRepository historyRepo = new HistoryRecordRepository(new StockMarketContext());
        IAssetReader reader = new YahooHtmlPageReader();
        List<Task<Asset>> tasks = [];
        foreach (var ticker in Setup.Tickers)
        {
            tasks.Add(reader.GetAssetAsync(ticker));
        }
        var assets = await Task.WhenAll(tasks);
        foreach (var asset in assets)
        {
            historyRepo.Insert(HistoryRecordAssetMapper.Map(asset));
        }
        historyRepo.Save();
    }

    /// <summary>
    /// Gets today's data from DB and displays in the console.
    /// </summary>
    public static void SeeRecentData()
    {
        using IRepository<HistoryRecord> historyRepo = new HistoryRecordRepository(new StockMarketContext());
        var historyRecords = historyRepo
            .Get(h => h.Date.Date == DateTime.UtcNow.Date)
            .DistinctBy(h => h.Ticker)
            .OrderByDescending(h => h.Change);
        WriteHistory(historyRecords);
    }

    /// <summary>
    /// Shows list of <see cref="HistoryRecord"/> in the console.
    /// </summary>
    private static void WriteHistory(IEnumerable<HistoryRecord> history)
    {
        WriteLine("Ticker", "Date", "Market Cap", "Current Price", "Future Price", "Change");
        Console.WriteLine(new string('-', 91));
        foreach (var record in history)
        {
            WriteLine(
                record.Ticker,
                record.Date.Date.ToShortDateString(),
                record.MarketCap.ToString(CultureInfo.CurrentCulture),
                record.CurrentPrice.ToString(CultureInfo.CurrentCulture),
                record.FuturePrice.ToString(CultureInfo.CurrentCulture),
                record.Change.ToString("F2"));
        }
    }

    /// <summary>
    /// Shows one line in the console.
    /// </summary>
    private static void WriteLine(string? ticker, string date, string marketCap, string currentPrice,
        string futurePrice, string change)
    {
        Console.WriteLine(
            $"{ticker,-10} | {date,10} | {marketCap,20} | {currentPrice,15} | {futurePrice,15} | {change,5}");
    }

    /// <summary>
    /// Reads info for the one ticker and saves the data to the DB, after displays in the console.
    /// </summary>
    /// <param name="ticker"></param>
    public static async Task SeeTicker(string? ticker)
    {
        if (ticker != null)
        {
            await using HistoryRecordRepository historyRepo = new HistoryRecordRepository(new StockMarketContext());
            IAssetReader reader = new YahooHtmlPageReader();
            Asset? asset = null;
            try
            {
                asset = await reader.GetAssetAsync(ticker);
            }
            catch (Exception)
            {
                Console.WriteLine("No price for ticker");
            }
            if (asset != null)
            {
                var historyRecord = HistoryRecordAssetMapper.Map(asset);
                historyRepo.Insert(historyRecord);
                historyRepo.Save();
                WriteHistory([historyRecord]);
            }
        }
    }
}