using FundMarket.Database.Models;

namespace FundMarket.Database.Repository;

public class AssetPurchaseRepository(StockMarketContext stockMarketContext) 
    : Repository<AssetPurchase>(stockMarketContext);