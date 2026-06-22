using CommunityToolkit.Mvvm.ComponentModel;

namespace FundMarket.Desktop.Models;

public partial class Ticker : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isIgnored;

    [ObservableProperty]
    private decimal _expectedPercent;

    [ObservableProperty]
    private decimal _currentPrice;

    public Ticker() { }

    public Ticker(string name)
    {
        Name = name;
    }
}