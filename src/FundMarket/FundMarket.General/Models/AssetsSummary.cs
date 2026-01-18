using System.Text;

namespace FundMarket.Helper.Models;

public class AssetsSummary
{
    public List<AssetSummaryItem> Assets { get; } = [];
    
    public decimal? TotalValue { get; set; } 
    
    public decimal? TotalPnL { get; set; } 
    
    public string? BestAsset { get; set; } 
    
    public string? WorstAsset { get; set; }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (var asset in Assets)
        {
            builder.AppendLine(asset.ToString());
        }
        builder.AppendLine($"Total Value: {TotalValue}");
        builder.AppendLine($"Total PnL: {TotalPnL}");
        builder.AppendLine($"Best Asset: {BestAsset}");
        builder.AppendLine($"Worst Asset: {WorstAsset}");
        return builder.ToString();
    }
}