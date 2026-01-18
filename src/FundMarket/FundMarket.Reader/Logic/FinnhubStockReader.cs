using System.Text.Json;
using FundMarket.Reader.Model;

namespace FundMarket.Reader.Logic;

public class FinnhubStockReader : IAssetReader
{
    private const int ApiRequestPerMinute = 58;

    private int _requestCount = 0;
    private static readonly Lock Lock = new();
    private static readonly HttpClient HttpClient = new();
    private readonly string _apiKey;

    public FinnhubStockReader(string? apiKey)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        _apiKey = apiKey;
    }

    public Asset GetAsset(string ticker) => GetAssetAsync(ticker).Result;

    public async Task<Asset> GetAssetAsync(string ticker)
    {
        lock (Lock)
        {
            if (_requestCount >= ApiRequestPerMinute)
            {
                // wait due to Response status code does not indicate success: 429 (Too Many Requests) error
                Thread.Sleep(TimeSpan.FromMinutes(1));
                _requestCount = 0;
            }
            // we have two calls in the method
            _requestCount += 2;
        }
        var price = await GetPrice(ticker);
        var marketCap = await GetMarketCap(ticker);
        return new Asset(ticker, price, price, marketCap);
    }

    public IEnumerable<Asset> GetAssets(IEnumerable<string> tickers)
    {
        return tickers.Select(GetAsset);
    }

    public async Task<IEnumerable<Asset>> GetAssetsAsync(IEnumerable<string> tickers)
    {
        List<Task<Asset>> tasks = new();
        foreach (var ticker in tickers)
        {
            tasks.Add(GetAssetAsync(ticker));
        }
        var results = await Task.WhenAll(tasks);
        return results;
    }

    private async Task<decimal> GetPrice(string ticker)
    {
        const string priceSymbol = "c";
        decimal price = decimal.Zero;
        string priceUrl = $"https://finnhub.io/api/v1/quote?symbol={ticker.ToUpper()}&token={_apiKey}";
        var priceResponse = await HttpClient.GetStringAsync(priceUrl);
        using var priceJson = JsonDocument.Parse(priceResponse);
        if (priceJson.RootElement.TryGetProperty(priceSymbol, out JsonElement priceElement))
        {
            price = priceElement.GetDecimal();
        }
        if (price == decimal.Zero)
        {
            throw new Exception($"Price not found for {ticker}");
        }
        return price;
    }

    private async Task<decimal> GetMarketCap(string ticker)
    {
        const string marketCapSymbol = "marketCapitalization";
        decimal marketCap = decimal.Zero;
        if (Constants.MarketCap.ETF.TryGetValue(ticker, out var cap))
        {
            marketCap = cap;
        }
        else
        {
            string marketUrl = $"https://finnhub.io/api/v1/stock/profile2?symbol={ticker.ToUpper()}&token={_apiKey}";
            var marketResponse = await HttpClient.GetStringAsync(marketUrl);
            using var marketJson = JsonDocument.Parse(marketResponse);
            if (marketJson.RootElement.TryGetProperty(marketCapSymbol, out var capElement))
            {
                marketCap = capElement.GetDecimal() * 1_000_000;
            }
        }
        if (marketCap == decimal.Zero)
        {
            throw new Exception($"Market Capitalization not found for {ticker}");
        }
        return marketCap;
    }
}