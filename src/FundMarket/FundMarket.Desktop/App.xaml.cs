using System.IO;
using System.Windows;
using FundMarket.Database;
using FundMarket.Desktop.ViewModels;
using FundMarket.Desktop.Views;
using FundMarket.Helper;
using FundMarket.Reader.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddDbContext<StockMarketContext>(options =>
            options.UseSqlite("Data Source=StockMarket.db"));
        services.AddSingleton<UnitOfWork>();
        // to do something with it ...
        services.AddSingleton<TextWriter>(_ => Console.Out);
        
        services.AddSingleton<StockService>();
        services.AddSingleton<IAssetReader>(_ =>
        {
            var apiKey = Environment.GetEnvironmentVariable("FinnhubStockApiKey");
            return new FinnhubStockReader(apiKey);
        });
        // Register ViewModels + Views
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();
        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainWindowViewModel = Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.Show();
    }
}