using CommunityToolkit.Mvvm.ComponentModel;

namespace FundMarket.Desktop.Models;

public partial class SellOrder : ObservableObject
{
    [ObservableProperty]
    private string _asset = string.Empty;

    [ObservableProperty]
    private decimal _qty = decimal.One;

    [ObservableProperty]
    private decimal _price = decimal.Zero;

    [ObservableProperty]
    private DateTime _date = DateTime.Now;

    [ObservableProperty]
    private string _status = string.Empty;
}