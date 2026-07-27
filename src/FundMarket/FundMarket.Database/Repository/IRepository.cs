using System.Linq.Expressions;

namespace FundMarket.Database.Repository;

public interface IRepository<T> : IAsyncDisposable where T : class
{
    IEnumerable<T> Get();

    IEnumerable<T> Get(Expression<Func<T, bool>> predicate);

    void Insert(T record);

    void Delete(T record);

    void DeleteRange(IEnumerable<T> records);

    void Update(T record);
}