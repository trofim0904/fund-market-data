namespace FundMarket.Core.Models;

public sealed class TickerExportItem
{
    public string Name { get; init; } = string.Empty;

    public bool IsIgnored { get; init; }

    public decimal ExpectedPercent { get; init; }

    public decimal CurrentPrice { get; init; }
}
