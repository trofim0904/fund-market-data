using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Database.Models;
using FundMarket.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public partial class BuyViewModel(IStockService stockService, ILogger<BuyViewModel> logger) : BaseViewModel(logger)
{
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

    public virtual async Task InitializeAsync()
    {
        var buyOrders = await stockService.GetAllBuyOrders();
        BuyOrders = new ObservableCollection<BuyOrder>(Map(buyOrders.OrderByDescending(o => o.Date)));
    }

    private static IEnumerable<BuyOrder> Map(IEnumerable<AssetPurchase> buyOrders)
    {
        return buyOrders.Select(b => new BuyOrder
        {
            Id = b.Id, Asset = b.Ticker!, Date = b.Date, Qty = b.Qty, Price = b.Price
        });
    }
}
