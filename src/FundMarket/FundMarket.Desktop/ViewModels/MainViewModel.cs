using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Core.Models;
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
    private BuyOrder? _buyOrder = new();

    [RelayCommand]
    private async Task BuyAssetAsync()
    {
        if (BuyOrder is null)
        {
            return;
        }
        await stockService.BuyAssetAsync(BuyOrder.Asset.ToUpper(), BuyOrder.Qty, BuyOrder.Price, BuyOrder.Date);
        BuyOrder = new BuyOrder
        {
            Status = "Completed"
        };
    }
    #endregion
    
    #region Buy Tab
    [ObservableProperty] 
    private SellOrder? _sellOrder = new();

    [RelayCommand]
    private async Task SellAssetAsync()
    {
        if (SellOrder is null)
        {
            return;
        }
        await stockService.SellAssetAsync(SellOrder.Asset.ToUpper(), SellOrder.Qty, SellOrder.Price, SellOrder.Date);
        SellOrder = new SellOrder
        {
            Status = "Completed"
        };
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
    #endregion
}