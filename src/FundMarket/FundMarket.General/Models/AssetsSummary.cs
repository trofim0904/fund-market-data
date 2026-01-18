namespace FundMarket.Helper.Models;

public class AssetsSummary
{
    // ReSharper disable once CollectionNeverQueried.Global
    public List<AssetSummaryItem> Assets { get; } = [];
    
    public decimal? TotalValue { get; set; } 
    
    public decimal? TotalPnL { get; set; } 
    
    public string? BestAsset { get; set; } 
    
    public string? WorstAsset { get; set; }
}