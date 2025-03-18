using System.Linq.Expressions;
using FundMarket.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace FundMarket.Database.Repository;

public class HistoryRecordRepository : IRepository<HistoryRecord>, IAsyncDisposable
{
    private readonly StockMarketContext _stockMarketContext;

    public HistoryRecordRepository(StockMarketContext stockMarketContext)
    {
        _stockMarketContext = stockMarketContext;
    }

    public IEnumerable<HistoryRecord> Get() => _stockMarketContext.HistoryRecords.ToList();

    public IEnumerable<HistoryRecord> Get(Expression<Func<HistoryRecord, bool>> predicate) =>
        _stockMarketContext.HistoryRecords.Where(predicate).ToList();

    public void Insert(HistoryRecord record)
    {
        _stockMarketContext.HistoryRecords.Add(record);
    }

    public void Delete(HistoryRecord record)
    {
        _stockMarketContext.HistoryRecords.Remove(record);
    }

    public void Update(HistoryRecord record)
    {
        _stockMarketContext.Entry(record).State = EntityState.Modified;
    }

    public void Save()
    {
        _stockMarketContext.SaveChanges();
    }

    public void Dispose()
    {
        _stockMarketContext.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stockMarketContext.DisposeAsync();
    }
}