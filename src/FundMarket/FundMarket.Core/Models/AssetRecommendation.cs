namespace FundMarket.Core.Models;

public class AssetRecommendation(string ticker, string reason, decimal qty = decimal.One)
{
    public string Ticker { get; } = ticker;

    public decimal Qty { get; } = qty;

    public string Reason { get; } = reason;

    public bool NewAsset { get; init; }
}