using FundMarket;

string? userInput;
do
{
    Console.WriteLine("Welcome to Fund Market Console App");
    Console.WriteLine("1. Load More Data");
    Console.WriteLine("2. See Recent Data");
    Console.WriteLine("3. See Ticker");
    Console.WriteLine("9. Clear Console");
    Console.WriteLine("0. Exit");
    userInput = Console.ReadLine();
    switch (userInput)
    {
        case "0":
            break;
        case "1":
            await StockService.LoadData();
            break;
        case "2":
            StockService.SeeRecentData();
            break;
        case "3":
            Console.Write("Input ticker: ");
            await StockService.SeeTicker(Console.ReadLine());
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