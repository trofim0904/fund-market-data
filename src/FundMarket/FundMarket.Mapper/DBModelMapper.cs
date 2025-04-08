using FundMarket.Database.Models;
using FundMarket.Reader.Model;

namespace FundMarket.Mapper;

public static class DBModelMapper
{
    public static HistoryRecord MapHistoryRecord(Asset asset)
    {
        return new HistoryRecord
        {
            Id = Guid.NewGuid(),
            Ticker = asset.Ticker,
            Date = DateTime.UtcNow,
            Change = asset.Change,
            CurrentPrice = asset.CurrentPrice,
            FuturePrice = asset.FuturePrice,
            MarketCap = asset.MarketCap
        };
    }

    public static Ticker MapTickerRecord(Asset asset)
    {
        return new Ticker
        {
            Id = Guid.NewGuid(), Name = asset.Ticker,
        };
    }

    public static AssetPurchase MapPurchase(string? ticker, decimal qtyDecimal, decimal priceDecimal, DateTime dateTime)
    {
        return new AssetPurchase
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Qty = qtyDecimal,
            Price = priceDecimal,
            Date = dateTime,
        };
    }
}