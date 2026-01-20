using FundMarket.Database.Models;
using FundMarket.Database.Repository;

namespace FundMarket.Database;

public interface IUnitOfWork : IDisposable
{
    public IRepository<Ticker> TickerRepository { get; }

    public IRepository<AssetPurchase> PurchaseRepository { get; }
    
    public IRepository<AssetSale> SaleRepository { get; } 

    Task<int> SaveChangesAsync();
}