using FundMarket;
using FundMarket.Database;
using Microsoft.EntityFrameworkCore;

DbContextOptionsBuilder optionsBuilder = new();
optionsBuilder.UseSqlite("Data Source=StockMarket.db");
StockMarketContext context = new StockMarketContext(optionsBuilder.Options);
UnitOfWork unitOfWork = new UnitOfWork(context);
StockConsoleService stockService = new StockConsoleService(unitOfWork);
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
            await stockService.AddTickersAsync(Console.ReadLine());
            break;
        case "2":
            stockService.SeeTickers();
            break;
        case "3":
            break;
        case "4":
            await stockService.SeeCurrentData();
            break;
        case "5":
            Console.Write("Input ticker: ");
            await stockService.SeeTickerData(Console.ReadLine());
            break;
        case "6":
            Console.Write("Input ticker: ");
            string? ticker = Console.ReadLine();
            Console.Write("Input qty: ");
            string? qty = Console.ReadLine();
            Console.Write("Input price: ");
            string? price = Console.ReadLine();
            Console.Write("Input date: ");
            string? date = Console.ReadLine();
            await stockService.BuyAsset(ticker, qty, price, date);
            break;
        case "7":
            stockService.SeeBoughtAssets();
            break;
        case "8":
            Console.WriteLine("Input amount to invest");
            await stockService.RecommendAssetsAsync(Console.ReadLine());
            break;
        case "9":
            Console.Clear();
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
    Console.WriteLine("4. See Current Market Data");
    Console.WriteLine("5. See Ticker");
    Console.WriteLine("6. Buy Asset");
    Console.WriteLine("7. See Bought Assets");
    Console.WriteLine("8. Recommend Assets to Buy");
    Console.WriteLine("9. Clear Console");
    Console.WriteLine("0. Exit");
}
