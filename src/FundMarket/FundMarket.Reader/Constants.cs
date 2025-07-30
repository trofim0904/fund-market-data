namespace FundMarket.Reader;

public static class Constants
{
    public static class MarketCap
    {
        public static readonly Dictionary<string, decimal> ETF = new()
        {
            { "SPY", 510_000_000_000 },
            { "BND", 100_000_000_000 },
            { "HDV", 10_000_000_000 },
            { "VNQ", 32_000_000_000 },
            { "SCHD", 55_000_000_000 },
            { "JEPI", 41_000_000_000 },
        };
    }
}