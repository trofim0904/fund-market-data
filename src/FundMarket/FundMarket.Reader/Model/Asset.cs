namespace FundMarket.Reader.Model;

public class Asset(string ticker, decimal currentPrice, decimal futurePrice)
{
    public string Ticker { get; } = ticker;

    public decimal CurrentPrice { get; } = currentPrice;

    public decimal FuturePrice { get; } = futurePrice;

    public decimal Change => CurrentPrice != decimal.Zero 
        ? (FuturePrice - CurrentPrice) / CurrentPrice * 100 
        : decimal.Zero;

    public override string ToString()
    {
        return $"Asset: {Ticker,-5} | Current Price: {CurrentPrice,7:F2} | Future Price: {FuturePrice,7:F2} | " 
               + (Change == decimal.Zero
                   ? string.Empty 
                   : $" | Change: {Change,4:F2}%");
    }
}