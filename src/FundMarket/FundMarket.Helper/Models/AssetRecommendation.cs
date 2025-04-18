namespace FundMarket.Helper.Models;

public class AssetRecommendation(string ticker, decimal qty = decimal.One)
{
    public string Ticker { get; } = ticker;

    public decimal Qty { get; } = qty;
}