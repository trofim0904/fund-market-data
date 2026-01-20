using CommunityToolkit.Mvvm.ComponentModel;

namespace FundMarket.Desktop.Models;

public partial class Recommendation : ObservableObject
{
    [ObservableProperty]
    private string _ticker = string.Empty;

    [ObservableProperty]
    private decimal _qty;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private bool _newAsset;
}