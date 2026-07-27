using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Core.Models;
using FundMarket.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public partial class RecommendationsViewModel(IStockDataRecommendationService recommendationService,
    ILogger<RecommendationsViewModel> logger) 
    : BaseViewModel(logger)
{
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
}
