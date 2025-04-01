using System.Globalization;
using FundMarket.Database;
using FundMarket.Database.Models;
using FundMarket.Mapper;
using FundMarket.Reader.Logic;
using FundMarket.Reader.Model;

namespace FundMarket;

public class StockService(UnitOfWork unitOfWork)
{
    /// <summary>
    /// Loads all tickers data specified in <see cref="Setup.Tickers"/> and saves to the DB.
    /// </summary>
    public async Task LoadData()
    {
        IAssetReader reader = new YahooHtmlPageReader();
        List<Task<Asset>> tasks = [];
        foreach (var ticker in Setup.Tickers)
        {
            tasks.Add(reader.GetAssetAsync(ticker));
        }
        var assets = await Task.WhenAll(tasks);
        foreach (var asset in assets)
        {
            unitOfWork.HistoryRepository.Insert(HistoryRecordAssetMapper.Map(asset));
        }
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Gets today's data from DB and displays in the console.
    /// </summary>
    public void SeeRecentData()
    {
        var historyRecords = unitOfWork.HistoryRepository
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
    public async Task SeeTicker(string? ticker)
    {
        if (ticker != null)
        {
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
                unitOfWork.HistoryRepository.Insert(historyRecord);
                await unitOfWork.SaveChangesAsync();
                WriteHistory([historyRecord]);
            }
        }
    }

    public async Task BuyAsset(string? ticker, string? qty, string? price, string? date)
    {
        Console.WriteLine("Not yet created");
    }

    public async Task SeeBoughtAssets()
    {
        Console.WriteLine("Not yet created");
    }
}