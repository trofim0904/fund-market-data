using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FundMarket.Desktop.Models;

public class Ticker : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isIgnored;
    private decimal _expectedPercent;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsIgnored
    {
        get => _isIgnored;
        set { _isIgnored = value; OnPropertyChanged(); }
    }

    public decimal ExpectedPercent
    {
        get => _expectedPercent;
        set { _expectedPercent = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Ticker() { }

    public Ticker(string name)
    {
        Name = name;
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}