using FundMarket.Database.Models;

namespace FundMarket.Database.Repository;

public class TickerRepository(StockMarketContext stockMarketContext) 
    : Repository<Ticker>(stockMarketContext);