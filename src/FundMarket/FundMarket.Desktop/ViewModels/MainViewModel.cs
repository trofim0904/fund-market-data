using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Desktop.Models;
using FundMarket.Helper;
using FundMarket.Helper.Models;
using FundMarket.Reader.Logic;
using Ticker = FundMarket.Desktop.Models.Ticker;
using EFTicker = FundMarket.Database.Models.Ticker;

namespace FundMarket.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly StockService _stockService;
    private readonly IAssetReader _reader;

    #region ctor
    public MainViewModel(StockService stockService, IAssetReader reader)
    {
        _stockService = stockService;
        _reader = reader;
        Tickers = new ObservableCollection<Ticker>(Map(stockService.GetAllTickers()));
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
        await _stockService.AddTickersAsync(NewTicker, _reader);
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
        await _stockService.DeleteTicker(SelectedTicker.Name);
        Tickers.Remove(SelectedTicker);
    }

    [RelayCommand]
    private async Task SaveTickerAsync()
    {
        if (SelectedTicker == null)
        {
            return;
        }
        await _stockService.UpdateExpectedPercent(SelectedTicker.Name, SelectedTicker.ExpectedPercent);
        await _stockService.UpdateIgnoreFlag(SelectedTicker.Name, SelectedTicker.IsIgnored);
    }

    [RelayCommand]
    private async Task SaveAllTickersAsync()
    {
        foreach (var ticker in Tickers)
        {
            await _stockService.UpdateExpectedPercent(ticker.Name, ticker.ExpectedPercent);
            await _stockService.UpdateIgnoreFlag(ticker.Name, ticker.IsIgnored);
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
            AssetSummary = await _stockService.GetBoughtAssets(_reader);
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
    private ObservableCollection<AssetRecommendation> _recommendations = [];

    [RelayCommand]
    private async Task RecommendAssetsAsync()
    {
        try 
        { 
            Recommendations =
                new ObservableCollection<AssetRecommendation>(
                    await _stockService.GetAssetRecommendation(AmountToInvest, _reader));
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
        await _stockService.BuyAsset(BuyOrder.Asset.ToUpper(), BuyOrder.Qty, BuyOrder.Price, BuyOrder.Date);
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
        await _stockService.SellAsset(SellOrder.Asset.ToUpper(), SellOrder.Qty, SellOrder.Price, SellOrder.Date);
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
    #endregion
}