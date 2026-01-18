namespace FundMarket.Reader.Model;

public class Asset(string ticker, decimal currentPrice)
{
    public string Ticker { get; } = ticker;

    public decimal CurrentPrice { get; } = currentPrice;
}