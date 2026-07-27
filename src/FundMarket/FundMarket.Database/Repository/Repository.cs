using System.Linq.Expressions;

namespace FundMarket.Database.Repository;

public class Repository<T>(StockMarketContext stockMarketContext) : IRepository<T> where T : class
{
    public IEnumerable<T> Get() => 
        stockMarketContext.Set<T>();

    public IEnumerable<T> Get(Expression<Func<T, bool>> predicate) => 
        stockMarketContext.Set<T>().Where(predicate);

    public void Insert(T record)
    {
        stockMarketContext.Set<T>().Add(record);
    }

    public void Delete(T record)
    {
        stockMarketContext.Set<T>().Remove(record);
    }

    public void DeleteRange(IEnumerable<T> records)
    {
        stockMarketContext.Set<T>().RemoveRange(records);
    }

    public void Update(T record)
    {
        stockMarketContext.Set<T>().Update(record);
    }

    public void Save()
    {
        stockMarketContext.SaveChanges();
    }
    
    public Task SaveAsync()
    {
        return stockMarketContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
       await stockMarketContext.DisposeAsync();
    }
}