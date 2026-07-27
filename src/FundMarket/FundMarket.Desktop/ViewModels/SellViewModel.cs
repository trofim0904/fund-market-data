using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Database.Models;
using FundMarket.Desktop.Models;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public partial class SellViewModel(IStockService stockService, ILogger<SellViewModel> logger) : BaseViewModel(logger)
{
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

    public virtual async Task InitializeAsync()
    {
        var sellOrders = await stockService.GetAllSellOrders();
        SellOrders = new ObservableCollection<SellOrder>(Map(sellOrders.OrderByDescending(o => o.Date)));
    }

    private IEnumerable<SellOrder> Map(IEnumerable<AssetSale> sellOrders)
    {
        return sellOrders.Select(b => new SellOrder
        {
            Id = b.Id, Asset = b.Ticker!, Date = b.Date, Qty = b.Qty, Price = b.Price
        });
    }
}
