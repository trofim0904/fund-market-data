namespace FundMarketLibrary.Model;

public class Asset(string ticker, decimal currentPrice, decimal futurePrice, string marketCap)
{
    public string Ticker { get; } = ticker;

    public decimal CurrentPrice { get; } = currentPrice;

    public decimal FuturePrice { get; } = futurePrice;

    public string MarketCap { get; } = marketCap;

    public decimal Change => (FuturePrice - CurrentPrice) / CurrentPrice * 100;
}