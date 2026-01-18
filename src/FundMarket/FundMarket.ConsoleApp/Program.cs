using FundMarket.Database;
using FundMarket.Helper;
using FundMarket.Reader.Logic;
using Microsoft.EntityFrameworkCore;

// setup for testing
string? apiKey = Environment.GetEnvironmentVariable("FinnhubStockApiKey");
DbContextOptionsBuilder optionsBuilder = new();
optionsBuilder.UseSqlite("Data Source=StockMarket.db");
await using StockMarketContext context = new StockMarketContext(optionsBuilder.Options);
UnitOfWork unitOfWork = new UnitOfWork(context);
// ReSharper disable once UnusedVariable
StockService stockService = new StockService(unitOfWork, Console.Out);
// ReSharper disable once UnusedVariable
IAssetReader reader = new FinnhubStockReader(apiKey);

// do your tests...
