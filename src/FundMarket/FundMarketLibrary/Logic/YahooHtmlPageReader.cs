using FundMarketLibrary.Model;
using HtmlAgilityPack;

namespace FundMarketLibrary.Logic;

public class YahooHtmlPageReader : IAssetReader
{
    const string YahooUrl = "https://finance.yahoo.com/";

    public Asset GetAsset(string ticker)
    {
        var defUrl = $"{YahooUrl}quote/{ticker}";
        var analysisUrl = $"{defUrl}/analysis";

        var web = new HtmlWeb();
        var analysisDoc = web.Load(analysisUrl);
        var futurePrice = GetFirstElementInnerText(analysisDoc, "//div[contains(@class, 'average')]");
        var currentPrice = GetFirstElementInnerText(analysisDoc, "//div[contains(@class, 'label yf-1i34qte')]");
        var defDoc = web.Load(defUrl);
        var marketCap = GetFirstElementInnerText(defDoc, "//div[@class='container yf-i6syij']//p[@class='value yf-i6syij']");
        return new Asset(ticker, decimal.Parse(currentPrice), decimal.Parse(futurePrice), marketCap);
    }

    public IEnumerable<Asset> GetAssets(IEnumerable<string> tickers)
    {
        return tickers.Select(GetAsset);
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
}