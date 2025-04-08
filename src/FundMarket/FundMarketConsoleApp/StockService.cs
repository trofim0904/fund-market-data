using System.Globalization;
using System.Text;
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
        foreach (var ticker in unitOfWork.TickerRepository.Get())
        {
            if (ticker.Name != null)
            {
                tasks.Add(reader.GetAssetAsync(ticker.Name));
            }
        }
        var assets = await Task.WhenAll(tasks);
        foreach (var asset in assets)
        {
            unitOfWork.HistoryRepository.Insert(DBModelMapper.MapHistoryRecord(asset));
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
    /// Reads info for the one ticker and displays in the console.
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
                var historyRecord = DBModelMapper.MapHistoryRecord(asset);
                WriteHistory([historyRecord]);
            }
        }
    }

    public async Task BuyAsset(string? ticker, string? qty, string? price, string? date)
    {
        ticker = ticker?.ToUpper();
        if (unitOfWork.TickerRepository.Get(t => t.Name == ticker).Any())
        {
            try
            {
                decimal.TryParse(qty, NumberStyles.Any, CultureInfo.InvariantCulture, out var qtyDecimal);
                decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceDecimal);
                DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime);
                unitOfWork.PurchaseRepository.Insert(DBModelMapper.MapPurchase(ticker,qtyDecimal, priceDecimal, dateTime));
                await unitOfWork.SaveChangesAsync();
                Console.WriteLine($"Added {qtyDecimal} of {ticker}");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

        }
        else
        {
            Console.WriteLine("Invalid ticker");
        }
    }

    public void SeeBoughtAssets()
    {
        var assets = unitOfWork.PurchaseRepository.Get().ToList();
        if (assets.Any())
        {
            var historyRecords = unitOfWork.HistoryRepository
                .Get(h => h.Date.Date == DateTime.UtcNow.Date)
                .DistinctBy(h => h.Ticker)
                .OrderByDescending(h => h.Change).ToList();
            var grouped = assets
                .GroupBy(a => a.Ticker)
                .Select(g => new
                {
                    Ticker = g.Key, 
                    Qty = g.Sum(a => a.Qty),
                    AvgPrice = GetAvgPrice(g.ToList()),
                    PurchaseTotal = g.Sum(a => a.Qty * a.Price)
                });
            Console.WriteLine($"{"Ticker",-10} | {"Qty",5} | {"Avg Price",10} | {"Purchase Total",15} | {"Current Price",15} | {"Current Total",15} | {"Diff",10}");
            var pnl = decimal.Zero;
            foreach (var total in grouped)
            {
                var history = historyRecords.FirstOrDefault(h => h.Ticker == total.Ticker);
                var currentPrice = history?.CurrentPrice ?? decimal.Zero;
                var currentTotal = currentPrice * total.Qty;
                var diff = currentTotal - total.PurchaseTotal;
                pnl += diff;
                Console.WriteLine($"{total.Ticker,-10} | {total.Qty,5} | {total.AvgPrice,10} | {total.PurchaseTotal,15} | {currentPrice,15} | {currentTotal,15} | {diff, 10}");
            }
            Console.WriteLine($"Total PnL: ${decimal.Round(pnl,2)}");
        }
    }

    private decimal GetAvgPrice(List<AssetPurchase> list)
    {
        decimal total = decimal.Zero;
        decimal totalQty = decimal.Zero;
        foreach (var purchase in list)
        {
            totalQty += purchase.Qty;
            total += purchase.Qty * purchase.Price;
        }
        return decimal.Round(total / totalQty, 3);
    }

    public async Task AddTickerAsync(string? input)
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
            foreach (var ticker in tickers)
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
                    if (unitOfWork.TickerRepository.Get(t => t.Name == asset.Ticker).Any())
                    {
                        Console.WriteLine("Ticker already exists");
                    }
                    else
                    {
                        unitOfWork.TickerRepository.Insert(DBModelMapper.MapTickerRecord(asset));
                        await unitOfWork.SaveChangesAsync();
                        Console.WriteLine("Ticker added");
                    }
                }
            }
        }
    }

    public void SeeTickers()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var ticker in unitOfWork.TickerRepository.Get())
        {
            builder.Append($"{ticker.Name} ");
        }
        Console.WriteLine(builder.ToString());
    }

    public async Task RecommendAssetsAsync(string? input)
    {
        try 
        {
            decimal.TryParse(input, out decimal amt);
            var tickets = unitOfWork.TickerRepository.Get().ToList();
            var purchases = unitOfWork.PurchaseRepository.Get().ToList();
            var historyRecords = unitOfWork.HistoryRepository.Get().ToList();
            // TODO: move to static class and return list of objects
            // all tickets must be bought
            var notBoughtTickets = tickets
                .Select(t => t.Name)
                .Except(purchases
                    .DistinctBy(p => p.Ticker)
                    .Select(p => p.Ticker))
                .ToList();
            if (notBoughtTickets.Count > 0)
            {
                foreach (var notBoughtTicket in notBoughtTickets)
                {
                    var lastHistoryRecord = historyRecords.FirstOrDefault(h => h.Ticker == notBoughtTicket);
                    if (lastHistoryRecord != null)
                    {
                        if (amt >= lastHistoryRecord.CurrentPrice)
                        {
                            var maxQty = Math.Floor(amt / lastHistoryRecord.CurrentPrice);
                            BuyTicket(notBoughtTicket, maxQty);
                            return;
                        }
                    }
                }
                Console.WriteLine("Cannot recommend assets. Increase invest amount");
                return;
            }
            // TODO: next logic step
            throw new NotImplementedException();
            Console.WriteLine("Cannot recommend assets. Increase invest amount");
        }
        catch (Exception)
        {
            //throw;
            Console.WriteLine("Error: not able to get assets");
        }
    }

    /// <summary>
    /// Outputs a recommendation to buy a specific stock (ticker) for the given amount.
    /// </summary>
    /// <param name="ticker">The stock ticker symbol (e.g., "AAPL", "TSLA").</param>
    /// <param name="qty">The recommended qty to invest.</param>
    private void BuyTicket(string? ticker, decimal qty)
    {
        Console.WriteLine($"Recommendation. Buy {qty} shares of ticker {ticker}.");
    }
}