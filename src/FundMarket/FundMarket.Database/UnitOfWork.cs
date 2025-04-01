using FundMarket.Database.Models;
using FundMarket.Database.Repository;

namespace FundMarket.Database;

public class UnitOfWork(StockMarketContext stockMarketContext)
{
    public IRepository<HistoryRecord> HistoryRepository = new HistoryRecordRepository(stockMarketContext);

    public void SaveChanges()
    {
        stockMarketContext.SaveChanges();
    }

    public Task SaveChangesAsync()
    {
        return stockMarketContext.SaveChangesAsync();
    }
}