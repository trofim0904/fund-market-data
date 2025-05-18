using FundMarket.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace FundMarket.Database;

public class StockMarketContext(DbContextOptions options) : DbContext(options)
{
    public virtual DbSet<Ticker> Tickers { get; set; }

    public virtual DbSet<AssetPurchase> AssetPurchases { get; set; }

    public virtual DbSet<AssetSale> AssetSales { get; set; }
}