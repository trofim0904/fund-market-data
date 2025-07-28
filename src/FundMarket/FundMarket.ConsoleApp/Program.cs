using FundMarket.Database;
using FundMarket.Helper;
using FundMarket.Reader.Logic;
using Microsoft.EntityFrameworkCore;

DbContextOptionsBuilder optionsBuilder = new();
optionsBuilder.UseSqlite("Data Source=StockMarket.db");
await using StockMarketContext context = new StockMarketContext(optionsBuilder.Options);
UnitOfWork unitOfWork = new UnitOfWork(context);
StockService stockService = new StockService(unitOfWork, Console.Out);
string? apiKey = Environment.GetEnvironmentVariable("FinnhubStockApiKey");
IAssetReader reader = new FinnhubStockReader(apiKey);
bool isRunning = true;

Console.WriteLine("Welcome to Fund Market Console App");
while (isRunning)
{
    PrintMenu();
    var userInput = Console.ReadLine();
    switch (userInput)
    {
        case "0":
            isRunning = false;
            break;
        case "1":
            Console.Write("Input ticker/tickers: ");
            await stockService.AddTickersAsync(Console.ReadLine(), reader);
            Pause();
            break;
        case "2":
            stockService.SeeTickers();
            Pause();
            break;
        case "3":
            Console.Write("Input ticker to update: ");
            var tickerToUpdate = Console.ReadLine();
            Console.Write("Do you want to update ignore flag? y/n: ");
            if (Console.ReadLine() == "y")
            {
                await stockService.UpdateIgnoreFlag(tickerToUpdate);
            }
            Console.Write("Do you want to delete ticker? y/n: ");
            if (Console.ReadLine() == "y")
            {
                await stockService.DeleteTicker(tickerToUpdate);
            }
            break;
        case "4":
            await stockService.SeeCurrentData(reader);
            Pause();
            break;
        case "5":
            Console.Write("Input ticker: ");
            await stockService.SeeTickerData(Console.ReadLine(), reader);
            Pause();
            break;
        case "6":
            await BuyAsset(stockService);
            Pause();
            break;
        case "7":
            await SellAsset(stockService);
            Pause();
            break;
        case "8":
            await stockService.SeeBoughtAssets(reader);
            Pause();
            break;
        case "9":
            Console.WriteLine("Input amount to invest");
            await stockService.RecommendAssetsAsync(Console.ReadLine(), reader);
            Pause();
            break;
        default:
            Console.WriteLine("Invalid input, please try again.");
            break;
    }
    Console.WriteLine();
}
return;

static void PrintMenu()
{
    Console.WriteLine("1. Add Ticker");
    Console.WriteLine("2. See Added Tickers");
    Console.WriteLine("3. Update Tickers");
    Console.WriteLine("4. See Current Market Data");
    Console.WriteLine("5. See Ticker");
    Console.WriteLine("6. Buy Asset");
    Console.WriteLine("7. Sell Asset");
    Console.WriteLine("8. See Bought Assets");
    Console.WriteLine("9. Recommend Assets to Buy");
    Console.WriteLine("0. Exit");
}

void Pause()
{
    Console.WriteLine("Enter any key to continue...");
    Console.ReadKey();
}

async Task BuyAsset(StockService stockConsoleService)
{
    Console.Write("Input ticker: ");
    string? ticker = Console.ReadLine();
    Console.Write("Input qty: ");
    string? qty = Console.ReadLine();
    Console.Write("Input price: ");
    string? price = Console.ReadLine();
    Console.Write("Input date (yyyy-MM-dd): ");
    string? date = Console.ReadLine();
    await stockConsoleService.BuyAsset(ticker, qty, price, date);
}

async Task SellAsset(StockService stockConsoleService)
{
    Console.Write("Input ticker: ");
    string? ticker = Console.ReadLine();
    Console.Write("Input qty: ");
    string? qty = Console.ReadLine();
    Console.Write("Input price: ");
    string? price = Console.ReadLine();
    Console.Write("Input date (yyyy-MM-dd): ");
    string? date = Console.ReadLine();
    await stockConsoleService.SellAsset(ticker, qty, price, date);
}
