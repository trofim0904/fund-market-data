using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Core.Models;
using FundMarket.Database.Models;
using FundMarket.Desktop.Models;
using Ticker = FundMarket.Desktop.Models.Ticker;
using EFTicker = FundMarket.Database.Models.Ticker;

namespace FundMarket.Desktop.ViewModels;

public partial class MainViewModel(
    IStockService stockService,
    IStockDataRecommendationService recommendationService) : ObservableObject
{
    #region Initialize Async
    public async Task InitializeAsync()
    {
        var tickers = await stockService.GetAllTickersAsync();
        Tickers = new ObservableCollection<Ticker>(Map(tickers));
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
    private Ticker? _selectedTicker;

    [RelayCommand]
    private async Task AddTickerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTicker))
        {
            return;
        }
        await stockService.AddTickerAsync(NewTicker);
        Tickers.Add(new Ticker(NewTicker.ToUpper()));
        NewTicker = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteTickerAsync()
    {
        if (SelectedTicker == null)
        {
            return;
        }
        await stockService.DeleteTickerAsync(SelectedTicker.Name);
        Tickers.Remove(SelectedTicker);
    }

    [RelayCommand]
    private async Task SaveTickerAsync()
    {
        if (SelectedTicker == null)
        {
            return;
        }
        await stockService.UpdateTickerAsync(SelectedTicker.Name, SelectedTicker.ExpectedPercent, SelectedTicker.IsIgnored);
    }

    [RelayCommand]
    private async Task SaveAllTickersAsync()
    {
        foreach (var ticker in Tickers)
        {
            await stockService.UpdateTickerAsync(ticker.Name, ticker.ExpectedPercent, ticker.IsIgnored);
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
            MessageBox.Show(e.Message);
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
            MessageBox.Show(e.Message);
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
        await stockService.BuyAssetAsync(BuyOrder.Asset.ToUpper(), BuyOrder.Qty, BuyOrder.Price, BuyOrder.Date);
        BuyOrders.Insert(0, BuyOrder);
        BuyOrder = new BuyOrder
        {
            Status = "Completed"
        };
    }

    [RelayCommand]
    private async Task DeleteHistoricalBuyOrderAsync()
    {
        try 
        { 
            await stockService.DeleteBuyAssetOrder(SelectedHistoricalBuyOrder.Id);
            BuyOrders.Remove(SelectedHistoricalBuyOrder);
        }
        catch (Exception)
        {
            MessageBox.Show("Cannot delete historical buy order");
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
        catch (Exception)
        {
            MessageBox.Show("Cannot update historical buy order");
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
        await stockService.SellAssetAsync(SellOrder.Asset.ToUpper(), SellOrder.Qty, SellOrder.Price, SellOrder.Date);
        SellOrders.Insert(0, SellOrder);
        SellOrder = new SellOrder
        {
            Status = "Completed"
        };
    }
    
    [RelayCommand]
    private async Task DeleteHistoricalSellOrderAsync()
    {
        try 
        { 
            await stockService.DeleteSellAssetOrder(SelectedHistoricalSellOrder.Id);
            SellOrders.Remove(SelectedHistoricalSellOrder);
        }
        catch (Exception)
        {
            MessageBox.Show("Cannot delete historical buy order");
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
        catch (Exception)
        {
            MessageBox.Show("Cannot update historical buy order");
        }
    }
    #endregion

    #region Internal Methods
    private static IEnumerable<Ticker> Map(IEnumerable<EFTicker> tickers)
    {
        return tickers.Select(ticker => new Ticker
        {
            Name = ticker.Name,
            ExpectedPercent = ticker.ExpectedPercent ?? decimal.Zero,
            IsIgnored = ticker.IsIgnored == true
        });
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
    #endregion
}