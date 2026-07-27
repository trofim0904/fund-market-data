using System.Windows;
using FundMarket.Core;
using FundMarket.Database;
using FundMarket.Desktop.ViewModels;
using FundMarket.Desktop.Views;
using FundMarket.Reader.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        services.AddDbContext<StockMarketContext>(options => options.UseSqlite("Data Source=StockMarket.db"));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStockService, StockDataService>();
        services.AddScoped<IStockDataRecommendationService, StockDataRecommendationService>();
        services.AddSingleton<TickerCsvExporter>();
        services.AddSingleton<IAssetReader>(_ => new FinnhubStockReader(Environment.GetEnvironmentVariable("FinnhubStockApiKey")));
        // Logger setup
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.AddFile("log");
            builder.SetMinimumLevel(LogLevel.Information);
        });
        // Register ViewModels + Views
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<TickersViewModel>();
        services.AddSingleton<BoughtAssetsViewModel>();
        services.AddSingleton<RecommendationsViewModel>();
        services.AddSingleton<BuyViewModel>();
        services.AddSingleton<SellViewModel>();
        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();
        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainWindowViewModel = Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.Show();
    }
}
