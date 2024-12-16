using System.Globalization;
using FundMarketLibrary.Logic;
using FundMarketLibrary.Model;
List<string> tickers = new List<string>
{
    "PLTR", "AMD", "NVDA", "BA", "CRSP",
    "PATH", "CHPT", "JPM", "NKE", "PFE",
    "LC", "ZG", "KO", "CHWY", "COP",
    "CVX", "WMT", "DIS", "VZ", "MMM"
};
IAssetReader reader = new YahooHtmlPageReader();
var assets = reader.GetAssets(tickers);
WriteAssets(assets.OrderByDescending(a => a.Change));
return;

static void WriteAssets(IEnumerable<Asset> assets)
{
    WriteLine("Ticker", "Market Cap", "Current Price", "Future Price", "Change");
    Console.WriteLine(new string('-', 68));
    foreach (var asset in assets)
    {
        WriteLine(
            asset.Ticker,
            asset.MarketCap,
            asset.CurrentPrice.ToString(CultureInfo.CurrentCulture),
            asset.FuturePrice.ToString(CultureInfo.CurrentCulture),
            asset.Change.ToString("F2"));
    }
}
static void WriteLine(string? ticker, string marketCap, string currentPrice, string futurePrice, string change)
{
    Console.WriteLine($"{ticker, -10} | {marketCap, 10} | {currentPrice, 15} | {futurePrice, 15} | {change, 5}");
}