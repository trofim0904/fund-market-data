using System.Text.Json;
using FundMarket.Reader.Model;

namespace FundMarket.Reader.Logic;

public class FinnhubStockReader : IAssetReader
{
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
        const string priceSymbol = "c";
        const string marketCapSymbol = "marketCapitalization";
        decimal price = decimal.Zero;
        decimal marketCap = decimal.Zero;
        string priceUrl = $"https://finnhub.io/api/v1/quote?symbol={ticker.ToUpper()}&token={_apiKey}";
        var priceResponse = await HttpClient.GetStringAsync(priceUrl);
        using var priceJson = JsonDocument.Parse(priceResponse);
        if (priceJson.RootElement.TryGetProperty(priceSymbol, out JsonElement priceElement))
        {
            price = priceElement.GetDecimal();
        }
        string marketUrl = $"https://finnhub.io/api/v1/stock/profile2?symbol={ticker.ToUpper()}&token={_apiKey}";
        var marketResponse = await HttpClient.GetStringAsync(marketUrl);
        using var marketJson = JsonDocument.Parse(marketResponse);
        if (marketJson.RootElement.TryGetProperty(marketCapSymbol, out var capElement))
        {
            marketCap = capElement.GetDecimal();
        }
        return new Asset(ticker, price, price, marketCap);
    }

    public IEnumerable<Asset> GetAssets(IEnumerable<string> tickers)
    {
        return tickers.Select(GetAsset);
    }

    public Task<IEnumerable<Asset>> GetAssetsAsync(IEnumerable<string> tickers)
    {
        return Task.Run(() => GetAssetsAsync(tickers));
    }
}