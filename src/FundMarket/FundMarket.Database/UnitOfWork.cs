using FundMarket.Database.Models;
using FundMarket.Database.Repository;

namespace FundMarket.Database;

public class UnitOfWork(StockMarketContext stockMarketContext)
{
    public IRepository<HistoryRecord> HistoryRepository { get; } = new HistoryRecordRepository(stockMarketContext);

    public IRepository<Ticker> TickerRepository { get; } = new TickerRepository(stockMarketContext);

    public IRepository<AssetPurchase> PurchaseRepository { get; } = new AssetPurchaseRepository(stockMarketContext);

    public void SaveChanges()
    {
        stockMarketContext.SaveChanges();
    }

    public Task SaveChangesAsync()
    {
        return stockMarketContext.SaveChangesAsync();
    }
}