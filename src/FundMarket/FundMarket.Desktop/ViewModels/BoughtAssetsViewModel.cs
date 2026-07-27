using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public partial class BoughtAssetsViewModel(IStockService stockService, ILogger<BoughtAssetsViewModel> logger)
    : BaseViewModel(logger)
{
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
}
