using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public partial class MainViewModel(
    ILogger<MainViewModel> logger,
    TickersViewModel tickersViewModel,
    BoughtAssetsViewModel boughtAssetsViewModel,
    RecommendationsViewModel recommendationsViewModel,
    BuyViewModel buyViewModel,
    SellViewModel sellViewModel) : BaseViewModel(logger)
{
    [ObservableProperty]
    private TickersViewModel _tickersViewModel = tickersViewModel;
    [ObservableProperty]
    private BoughtAssetsViewModel _boughtAssetsViewModel = boughtAssetsViewModel;
    [ObservableProperty]
    private RecommendationsViewModel _recommendationsViewModel = recommendationsViewModel;
    [ObservableProperty]
    private BuyViewModel _buyViewModel = buyViewModel;
    [ObservableProperty]
    private SellViewModel _sellViewModel = sellViewModel;

    public virtual async Task InitializeAsync()
    {
        try
        {
            await TickersViewModel.ReloadTickersAsync();
            await BuyViewModel.InitializeAsync();
            await SellViewModel.InitializeAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }
}
