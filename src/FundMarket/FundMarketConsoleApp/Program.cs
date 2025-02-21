using System.Globalization;
using FundMarketLibrary.Logic;
using FundMarketLibrary.Model;

List<string> tickers =
[
    "PLTR", "AMD", "NVDA", "BA", "CRSP",
    "PATH", "CHPT", "JPM", "NKE", "PFE",
    "LC", "ZG", "KO", "CHWY", "COP",
    "CVX", "WMT", "DIS", "VZ", "MMM"
];
IAssetReader reader = new YahooHtmlPageReader();
List<Task<Asset>> tasks = [];
foreach (var ticker in tickers)
{
    tasks.Add(reader.GetAssetAsync(ticker));
}
var assets = await Task.WhenAll(tasks);
WriteAssets(assets.OrderByDescending(a => a.MarketCap));
return;

static void WriteAssets(IEnumerable<Asset> assets)
{
    WriteLine("Ticker", "Market Cap", "Current Price", "Future Price", "Change");
    Console.WriteLine(new string('-', 78));
    foreach (var asset in assets)
    {
        WriteLine(
            asset.Ticker,
            asset.MarketCap.ToString(CultureInfo.CurrentCulture),
            asset.CurrentPrice.ToString(CultureInfo.CurrentCulture),
            asset.FuturePrice.ToString(CultureInfo.CurrentCulture),
            asset.Change.ToString("F2"));
    }
}

static void WriteLine(string? ticker, string marketCap, string currentPrice, string futurePrice, string change)
{
    Console.WriteLine($"{ticker, -10} | {marketCap, 20} | {currentPrice, 15} | {futurePrice, 15} | {change, 5}");
}