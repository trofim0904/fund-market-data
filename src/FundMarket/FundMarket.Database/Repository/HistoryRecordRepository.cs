using FundMarket.Database.Models;

namespace FundMarket.Database.Repository;

public class HistoryRecordRepository(StockMarketContext stockMarketContext) 
    : Repository<HistoryRecord>(stockMarketContext) { }