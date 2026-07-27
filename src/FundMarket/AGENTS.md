This is a C#/.NET application for tracking and analyzing stock market data. The solution is composed of several projects, each with a specific responsibility.

### Architecture

The application follows a layered architecture:

-   **`FundMarket.Desktop`**: The presentation layer, a WPF application that provides the user interface. It uses dependency injection to resolve services. The main window is `MainWindow` and its view model is `MainViewModel`.
-   **`FundMarket.ConsoleApp`**: A console application, primarily for testing and debugging.
-   **`FundMarket.Core`**: The business logic layer.
    -   `IStockService` (`StockDataService`): Manages stock data, including tickers, buy/sell orders, and portfolio summaries.
    -   `IStockDataRecommendationService` (`StockDataRecommendationService`): Provides recommendations for asset purchases based on a target allocation.
-   **`FundMarket.Database`**: The data access layer. It uses Entity Framework Core with a SQLite database (`StockMarket.db`). The `UnitOfWork` and Repository patterns are used to abstract data access.
-   **`FundMarket.Reader`**: A layer for fetching data from external sources.
    -   `IAssetReader` (`FinnhubStockReader`): Fetches stock data from the Finnhub API. An API key is required and is read from the `FinnhubStockApiKey` environment variable.

### Key Concepts

-   **Tickers**: These are the stock symbols (e.g., "AAPL", "GOOG"). They are stored in the database and have an "expected percent" which is used for portfolio balancing.
-   **Purchases and Sales**: All buy and sell orders are stored in the database.
-   **Recommendations**: The `StockDataRecommendationService` generates recommendations on which assets to buy to bring the portfolio closer to the desired allocation (defined by the "expected percent" of each ticker).

### Development Workflow

-   **Building**: The solution can be built using Visual Studio or the `dotnet build` command.
-   **Database**: The application uses a SQLite database named `StockMarket.db`. This file will be created in the output directory.
-   **API Key**: To run the application, you need a Finnhub API key. You must set the `FinnhubStockApiKey` environment variable to your key.

### Important Files

-   `FundMarket.Desktop/App.xaml.cs`: The entry point for the WPF application, where services are configured.
-   `FundMarket.Core/IStockService.cs`: Defines the contract for managing stock data.
-   `FundMarket.Core/IStockDataRecommendationService.cs`: Defines the contract for the recommendation service.
-   `FundMarket.Database/StockMarketContext.cs`: The Entity Framework `DbContext` for the application.
-   `FundMarket.Reader/Logic/FinnhubStockReader.cs`: The implementation for fetching data from Finnhub.

### How to run the project

To run the project, you need to set the `FinnhubStockApiKey` environment variable and then run the `FundMarket.Desktop` project.

```powershell
$env:FinnhubStockApiKey = "YOUR_API_KEY"
dotnet run --project FundMarket.Desktop/FundMarket.Desktop.csproj
```

