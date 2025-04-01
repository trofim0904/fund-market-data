using FundMarket;
using FundMarket.Database;
using Microsoft.EntityFrameworkCore;

string? userInput;

DbContextOptionsBuilder optionsBuilder = new();
optionsBuilder.UseSqlite("Data Source=StockMarket.db");
StockMarketContext context = new StockMarketContext(optionsBuilder.Options);
UnitOfWork unitOfWork = new UnitOfWork(context);
StockService stockService = new StockService(unitOfWork);

do
{
    Console.WriteLine("Welcome to Fund Market Console App");
    Console.WriteLine("1. Load More Data");
    Console.WriteLine("2. See Recent Data");
    Console.WriteLine("3. See Ticker");
    Console.WriteLine("4. Buy Asset");
    Console.WriteLine("5. See bought assets");
    Console.WriteLine("9. Clear Console");
    Console.WriteLine("0. Exit");
    userInput = Console.ReadLine();
    switch (userInput)
    {
        case "0":
            break;
        case "1":
            await stockService.LoadData();
            break;
        case "2":
            stockService.SeeRecentData();
            break;
        case "3":
            Console.Write("Input ticker: ");
            await stockService.SeeTicker(Console.ReadLine());
            break;
        case "4":
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
        case "5":
            await stockService.SeeBoughtAssets();
            break;
        case "9":
            Console.Clear();
            break;
        default:
            Console.WriteLine("Invalid Input");
            break;
    }
    Console.WriteLine();
} while (userInput != "0");