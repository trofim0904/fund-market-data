using FundMarket.Database.Models;
using FundMarket.Reader.Model;

namespace FundMarket.Mapper;

public static class HistoryRecordAssetMapper
{
    public static HistoryRecord Map(Asset asset)
    {
        return new HistoryRecord()
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
}