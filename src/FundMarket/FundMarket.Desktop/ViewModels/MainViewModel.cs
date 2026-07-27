using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Core.Models;
using FundMarket.Database.Models;
using FundMarket.Desktop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ticker = FundMarket.Desktop.Models.Ticker;

namespace FundMarket.Desktop.ViewModels;

public partial class MainViewModel(
    IStockService stockService,
    IStockDataRecommendationService recommendationService,
    TickerCsvExporter tickerCsvExporter,
    ILogger<MainViewModel> logger) : ObservableObject
{
    #region Initialize Async
    public async Task InitializeAsync()
    {
        await RefreshTickerDataAsync();
        var buyOrders = await stockService.GetAllBuyOrders();
        BuyOrders = new ObservableCollection<BuyOrder>(Map(buyOrders.OrderByDescending(o => o.Date)));
        var sellOrders = await stockService.GetAllSellOrders();
        SellOrders = new ObservableCollection<SellOrder>(Map(sellOrders.OrderByDescending(o => o.Date)));
    }
    #endregion

    #region Tickets Tab
    [ObservableProperty] 
    private string _newTicker = string.Empty;

    [ObservableProperty] 
    private ObservableCollection<Ticker> _tickers = [];
    
    [ObservableProperty] 
    private ObservableCollection<Ticker> _activeTickers = [];

    [ObservableProperty]
    private decimal _totalNotIgnoredPercent;

    [ObservableProperty]
    private int _totalNotIgnoredTickers;

    [ObservableProperty] 
    private Ticker? _selectedTicker;

    [RelayCommand]
    private async Task ReloadTickersAsync()
    {
        try
        {
            await RefreshTickerDataAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task ExportTickersAsCsvAsync()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"tickers-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            var csv = tickerCsvExporter.CreateCsv(Tickers.Select(ticker => new TickerExportItem
            {
                Name = ticker.Name,
                IsIgnored = ticker.IsIgnored,
                ExpectedPercent = ticker.ExpectedPercent,
                CurrentPrice = ticker.CurrentPrice
            }));
            await File.WriteAllTextAsync(dialog.FileName, csv, Encoding.UTF8);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task AddTickerAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewTicker))
            {
                return;
            }
            await stockService.AddTickerAsync(NewTicker);
            await RefreshTickerDataAsync();
            NewTicker = string.Empty;
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task DeleteTickerAsync()
    {
        try
        {
            if (SelectedTicker == null)
            {
                return;
            }
            await stockService.DeleteTickerAsync(SelectedTicker.Name);
            await RefreshTickerDataAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task SaveTickerAsync()
    {
        try
        {
            if (SelectedTicker == null)
            {
                return;
            }
            await stockService.UpdateTickerAsync(SelectedTicker.Name,
                SelectedTicker.ExpectedPercent, SelectedTicker.IsIgnored);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task SaveAllTickersAsync()
    {
        try
        {
            foreach (var ticker in Tickers)
            {
                await stockService.UpdateTickerAsync(ticker.Name, ticker.ExpectedPercent, ticker.IsIgnored);
            }
            await RefreshTickerDataAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    #endregion

    #region Bought Assets Tab
    [ObservableProperty] 
    private ObservableCollection<AssetSummaryItem> _boughtAssets = [];

    [ObservableProperty]
    private AssetsSummary _assetSummary = new();

    [RelayCommand]
    private async Task SeeBoughtAssetsAsync()
    {
        try 
        {
            AssetSummary = await stockService.GetBoughtAssetsAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    #endregion

    #region Recommendations Tab
    [ObservableProperty] 
    private decimal _amountToInvest = decimal.Zero;

    [ObservableProperty]
    private ObservableCollection<Recommendation> _recommendations = [];

    [RelayCommand]
    private async Task RecommendAssetsAsync()
    {
        try
        {
            var recommendation = await recommendationService.GetAssetRecommendationAsync(AmountToInvest);
            Recommendations = new ObservableCollection<Recommendation>(Map(recommendation));
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    #endregion

    #region Buy Tab
    [ObservableProperty] 
    private BuyOrder _buyOrder = new();
    
    [ObservableProperty] 
    private BuyOrder _selectedHistoricalBuyOrder = new();
    
    [ObservableProperty] 
    private ObservableCollection<BuyOrder> _buyOrders = [];

    [RelayCommand]
    private async Task BuyAssetAsync()
    {
        try
        {
            await stockService.BuyAssetAsync(BuyOrder.Asset.ToUpper(), BuyOrder.Qty, BuyOrder.Price, BuyOrder.Date);
            BuyOrders.Insert(0, BuyOrder);
            BuyOrder = new BuyOrder
            {
                Status = "Completed",
                Date = DateTime.Now
            };
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task SetBuyOrderPriceAsCurrentAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BuyOrder.Asset))
            {
                return;
            }
            BuyOrder.Price = await stockService.GetCurrentPriceAsync(BuyOrder.Asset);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task DeleteHistoricalBuyOrderAsync()
    {
        try 
        { 
            await stockService.DeleteBuyAssetOrder(SelectedHistoricalBuyOrder.Id);
            BuyOrders.Remove(SelectedHistoricalBuyOrder);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    
    [RelayCommand]
    private async Task SaveHistoricalBuyOrderAsync()
    {
        try 
        { 
            await stockService.UpdateBuyAssetOrder(
                SelectedHistoricalBuyOrder.Id,
                SelectedHistoricalBuyOrder.Asset.ToUpper(),
                SelectedHistoricalBuyOrder.Qty,
                SelectedHistoricalBuyOrder.Price,
                SelectedHistoricalBuyOrder.Date);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    #endregion
    
    #region Sell Tab
    [ObservableProperty] 
    private SellOrder _sellOrder = new();
    
    [ObservableProperty] 
    private SellOrder _selectedHistoricalSellOrder = new();
    
    [ObservableProperty] 
    private ObservableCollection<SellOrder> _sellOrders = [];

    [RelayCommand]
    private async Task SellAssetAsync()
    {
        try
        {
            await stockService.SellAssetAsync(SellOrder.Asset.ToUpper(), SellOrder.Qty, SellOrder.Price, SellOrder.Date);
            SellOrders.Insert(0, SellOrder);
            SellOrder = new SellOrder
            {
                Status = "Completed",
                Date = DateTime.Now
            };
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task SetSellOrderPriceAsCurrentAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SellOrder.Asset))
            {
                return;
            }
            SellOrder.Price = await stockService.GetCurrentPriceAsync(SellOrder.Asset);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    
    [RelayCommand]
    private async Task DeleteHistoricalSellOrderAsync()
    {
        try 
        { 
            await stockService.DeleteSellAssetOrder(SelectedHistoricalSellOrder.Id);
            SellOrders.Remove(SelectedHistoricalSellOrder);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    
    [RelayCommand]
    private async Task SaveHistoricalSellOrderAsync()
    {
        try 
        { 
            await stockService.UpdateSellAssetOrder(
                SelectedHistoricalSellOrder.Id,
                SelectedHistoricalSellOrder.Asset.ToUpper(),
                SelectedHistoricalSellOrder.Qty,
                SelectedHistoricalSellOrder.Price,
                SelectedHistoricalSellOrder.Date);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
    #endregion

    #region Internal Methods
    private async Task RefreshTickerDataAsync()
    {
        var tickers = (await stockService.GetAllTickersAsync()).ToList();
        var mappedTickers = new List<Ticker>();
        foreach (var ticker in tickers)
        {
            mappedTickers.Add(new Ticker
            {
                Name = ticker.Name,
                ExpectedPercent = ticker.ExpectedPercent ?? decimal.Zero,
                IsIgnored = ticker.IsIgnored == true,
                CurrentPrice = ticker.IsIgnored == true 
                    ? decimal.Zero 
                    : await stockService.GetCurrentPriceAsync(ticker.Name)
            });
        }
        Tickers = new ObservableCollection<Ticker>(mappedTickers);
        ActiveTickers = new ObservableCollection<Ticker>(mappedTickers.Where(t => t.IsIgnored != true));
        TotalNotIgnoredPercent = Math.Round(mappedTickers.Where(t => t.IsIgnored != true).Sum(t => t.ExpectedPercent), 2, MidpointRounding.ToEven);
        TotalNotIgnoredTickers = mappedTickers.Count(t => t.IsIgnored != true);
    }

    private static IEnumerable<Recommendation> Map(IEnumerable<AssetRecommendation> recommendation)
    {
        return recommendation.Select(assetRecommendation => new Recommendation
        {
            NewAsset = assetRecommendation.NewAsset,
            Ticker = assetRecommendation.Ticker,
            Qty = assetRecommendation.Qty,
            Reason = assetRecommendation.Reason
        });
    }

    private static IEnumerable<BuyOrder> Map(IEnumerable<AssetPurchase> buyOrders)
    {
        return buyOrders.Select(b => new BuyOrder
        {
            Id = b.Id, Asset = b.Ticker!, Date = b.Date, Qty = b.Qty, Price = b.Price
        });
    }
    
    private IEnumerable<SellOrder> Map(IEnumerable<AssetSale> sellOrders)
    {
        return sellOrders.Select(b => new SellOrder
        {
            Id = b.Id, Asset = b.Ticker!, Date = b.Date, Qty = b.Qty, Price = b.Price
        });
    }
    
    private void HandleError(Exception ex)
    {
        const string? errorOccured = "An error occured.";
        logger.LogError(ex, errorOccured);
        MessageBox.Show(
            "Something went wrong while processing the request.",
            "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    #endregion
}
