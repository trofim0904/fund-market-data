namespace FundMarket.Reader.Model;

public class Asset(string ticker, decimal currentPrice, decimal futurePrice, decimal marketCap)
{
    public string Ticker { get; } = ticker;

    public decimal CurrentPrice { get; } = currentPrice;

    public decimal FuturePrice { get; } = futurePrice;

    public decimal MarketCap { get; } = marketCap;

    public decimal Change => (FuturePrice - CurrentPrice) / CurrentPrice * 100;

    public override string ToString()
    {
        return $"Asset: {Ticker,-5} | Current Price: {CurrentPrice,7:F2} | Future Price: {FuturePrice,7:F2} | " +
               $"Market Cap: {MarketCap,17:N0} | Change: {Change,4:F2}%";
    }
}