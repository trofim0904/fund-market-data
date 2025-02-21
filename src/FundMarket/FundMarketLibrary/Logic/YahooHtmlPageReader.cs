using FundMarketLibrary.Model;
using HtmlAgilityPack;

namespace FundMarketLibrary.Logic;

public class YahooHtmlPageReader : IAssetReader
{
    private const string YahooUrl = "https://finance.yahoo.com/";

    public Asset GetAsset(string ticker)
    {
        var defUrl = $"{YahooUrl}quote/{ticker}";
        var analysisUrl = $"{defUrl}/analysis";
        var web = new HtmlWeb();
        var analysisDoc = web.Load(analysisUrl);
        var futurePrice = GetFirstElementInnerText(analysisDoc, "//div[contains(@class, 'average')]");
        var currentPrice = GetFirstElementInnerText(analysisDoc, "//div[contains(@class, 'label yf-1i34qte')]");
        var defDoc = web.Load(defUrl);
        var marketCap =
            GetFirstElementInnerText(defDoc, "//div[@class='container yf-i6syij']//p[@class='value yf-i6syij']");
        return new Asset(ticker, decimal.Parse(currentPrice), decimal.Parse(futurePrice), ConvertToDecimal(marketCap));
    }

    public Task<Asset> GetAssetAsync(string ticker)
    {
        return Task.Run(() => GetAsset(ticker));
    }

    public IEnumerable<Asset> GetAssets(IEnumerable<string> tickers)
    {
        return tickers.Select(GetAsset);
    }

    public Task<IEnumerable<Asset>> GetAssetsAssync(IEnumerable<string> tickers)
    {
        return Task.Run(() => GetAssets(tickers));
    }

    private static string GetFirstElementInnerText(HtmlDocument doc, string filter)
    {
        var nodes = doc.DocumentNode.SelectNodes(filter);
        foreach (HtmlNode node in nodes)
        {
            if (node != null)
            {
                string innerText = node.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(innerText))
                {
                    continue;
                }
                return innerText.Split(' ')[0];
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Converts market capitalization from string to decimal.
    /// </summary>
    /// <param name="marketCap">String value, ex: 262.82B, 3.41T</param>
    /// <returns>Decimal representation.</returns>
    private decimal ConvertToDecimal(string marketCap)
    {
        char? lastChar = marketCap.LastOrDefault();
        if (lastChar != null)
        {
            if (Descriptor.Constant.LargeNumberRepresentation.TryGetValue(lastChar.Value, out var multiplier))
            {
                if (decimal.TryParse(marketCap.Remove(marketCap.Length - 1), out var price))
                {
                    return price * multiplier;
                }
            }
            else
            {
                if (decimal.TryParse(marketCap, out var price))
                {
                    return price;
                }
            }
        }
        return decimal.Zero;
    }
}