namespace FundMarket.Helper.Models;

public class AssetSummary
{
    public string? Ticker { get; init; }

    public decimal? Qty { get; set; } 

    public decimal? AvgPrice { get; init; } 

    public decimal? PurchaseTotal { get; set; } 

    public decimal? CurrentPrice { get; init; }

    public decimal? CurrentValue => CurrentPrice * Qty;

    public decimal? Difference => CurrentValue - PurchaseTotal;

    public decimal? Percent { get; set; }

    public int? Tier { get; set; }

    public override string ToString()
    {
        return
            $"Ticker: {Ticker, -5} | " +
            $"Qty: {Qty,5:F2} | " +
            $"Avg Price: {AvgPrice,7:F2} | " +
            $"Purchase Total: {PurchaseTotal,10:F2} | " +
            $"Current Price: {CurrentPrice,7:F2} | " +
            $"Current Value: {CurrentValue,10:F2} | " +
            $"Difference: {Difference,7:F2}";
    }
}