using FundMarket.Core;
using FundMarket.Database;
using FundMarket.Reader.Logic;
using Microsoft.EntityFrameworkCore;

// setup for testing
string? apiKey = Environment.GetEnvironmentVariable("FinnhubStockApiKey");
DbContextOptionsBuilder optionsBuilder = new();
optionsBuilder.UseSqlite("Data Source=StockMarket.db");
await using StockMarketContext context = new StockMarketContext(optionsBuilder.Options);
IUnitOfWork unitOfWork = new UnitOfWork(context);
// ReSharper disable once UnusedVariable
IAssetReader reader = new FinnhubStockReader(apiKey);
// ReSharper disable once UnusedVariable
IStockService stockService = new StockDataService(unitOfWork, reader);

// do your tests...
