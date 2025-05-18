using FundMarket.Database.Models;

namespace FundMarket.Database.Repository;

public class AssetSalesRepository(StockMarketContext stockMarketContext) 
    : Repository<AssetSale>(stockMarketContext);