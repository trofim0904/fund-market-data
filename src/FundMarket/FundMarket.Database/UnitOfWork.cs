using FundMarket.Database.Models;
using FundMarket.Database.Repository;

namespace FundMarket.Database;

public class UnitOfWork(StockMarketContext stockMarketContext)
{
    public IRepository<Ticker> TickerRepository { get; } = new Repository<Ticker>(stockMarketContext);

    public IRepository<AssetPurchase> PurchaseRepository { get; } = new Repository<AssetPurchase>(stockMarketContext);

    public IRepository<AssetSale> SaleRepository { get; } = new Repository<AssetSale>(stockMarketContext);

    public Task SaveChangesAsync()
    {
        return stockMarketContext.SaveChangesAsync();
    }
}