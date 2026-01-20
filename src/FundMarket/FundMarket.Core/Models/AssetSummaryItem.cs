namespace FundMarket.Core.Models;

public class AssetSummaryItem
{
    public string? Ticker { get; init; }

    public decimal? Qty { get; set; } 

    public decimal? AvgPrice { get; init; } 

    public decimal? PurchaseTotal { get; set; } 

    public decimal? CurrentPrice { get; init; }

    public decimal? CurrentValue => CurrentPrice * Qty;

    public decimal? Difference => CurrentValue - PurchaseTotal;

    public decimal? Percent { get; set; }
    
    public decimal? ExpectedPercent { get; set; }
}