using System.Globalization;
using FundMarket.Reader.Model;
using HtmlAgilityPack;

namespace FundMarket.Reader.Logic;

[Obsolete("Stopped working, checked on 30/06/2025")]
public class YahooHtmlPageReader : IAssetReader
{
    private const string YahooUrl = "https://finance.yahoo.com/";

    public Asset GetAsset(string ticker)
    {
        var defUrl = $"{YahooUrl}quote/{ticker}";
        var analysisUrl = $"{defUrl}/analysis/";
        var web = new HtmlWeb();
        var analysisDoc = web.Load(analysisUrl);
        var futurePrice = GetFirstElementInnerText(analysisDoc, "//div[contains(@class, 'average')]");
        var currentPrice = GetFirstElementInnerText(analysisDoc, "//div[contains(@class, 'label yf-1i34qte')]");
        if (string.IsNullOrWhiteSpace(currentPrice))
        {
            currentPrice = decimal.Zero.ToString(CultureInfo.InvariantCulture);
        }
        if (string.IsNullOrWhiteSpace(futurePrice))
        {
            futurePrice = decimal.Zero.ToString(CultureInfo.InvariantCulture);
        }
        return new Asset(ticker, decimal.Parse(currentPrice), decimal.Parse(futurePrice));
    }

    public Task<Asset> GetAssetAsync(string ticker)
    {
        return Task.Run(() => GetAsset(ticker));
    }

    public IEnumerable<Asset> GetAssets(IEnumerable<string> tickers)
    {
        return tickers.Select(GetAsset);
    }

    public Task<IEnumerable<Asset>> GetAssetsAsync(IEnumerable<string> tickers)
    {
        return Task.Run(() => GetAssets(tickers));
    }

    private static string GetFirstElementInnerText(HtmlDocument doc, string filter)
    {
        var nodes = doc.DocumentNode.SelectNodes(filter);
        if (nodes == null)
        {
            return string.Empty;
        }
        foreach (HtmlNode node in nodes)
        {
            if (node == null)
            {
                continue;
            }
            var innerText = node.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(innerText))
            {
                continue;
            }
            return innerText.Split(' ')[0];
        }
        return string.Empty;
    }
}