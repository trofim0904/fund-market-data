using FundMarket.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace FundMarket.Database;

public class StockMarketContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=StockMarket.db");
    }

    public virtual DbSet<HistoryRecord> HistoryRecords { get; set; }
}