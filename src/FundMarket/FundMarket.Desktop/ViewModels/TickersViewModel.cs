using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FundMarket.Core;
using FundMarket.Core.Models;
using FundMarket.Desktop.Models;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public partial class TickersViewModel(IStockService stockService, 
    TickerCsvExporter tickerCsvExporter, ILogger<TickersViewModel> logger) 
    : BaseViewModel(logger)
{
    [ObservableProperty] 
    private string _newTicker = string.Empty;

    [ObservableProperty] 
    private ObservableCollection<Ticker> _tickers = [];
    
    [ObservableProperty] 
    private ObservableCollection<Ticker> _activeTickers = [];

    [ObservableProperty]
    private decimal _totalNotIgnoredPercent;

    [ObservableProperty]
    private int _totalNotIgnoredTickers;

    [ObservableProperty] 
    private Ticker? _selectedTicker;

    [RelayCommand]
    public async Task ReloadTickersAsync()
    {
        try
        {
            await RefreshTickerDataAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task ExportTickersAsCsvAsync()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"tickers-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            var csv = tickerCsvExporter.CreateCsv(Tickers.Select(ticker => new TickerExportItem
            {
                Name = ticker.Name,
                IsIgnored = ticker.IsIgnored,
                ExpectedPercent = ticker.ExpectedPercent,
                CurrentPrice = ticker.CurrentPrice
            }));
            await File.WriteAllTextAsync(dialog.FileName, csv, Encoding.UTF8);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task AddTickerAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewTicker))
            {
                return;
            }
            await stockService.AddTickerAsync(NewTicker);
            await RefreshTickerDataAsync();
            NewTicker = string.Empty;
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task DeleteTickerAsync()
    {
        try
        {
            if (SelectedTicker == null)
            {
                return;
            }
            await stockService.DeleteTickerAsync(SelectedTicker.Name);
            await RefreshTickerDataAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task SaveTickerAsync()
    {
        try
        {
            if (SelectedTicker == null)
            {
                return;
            }
            await stockService.UpdateTickerAsync(SelectedTicker.Name,
                SelectedTicker.ExpectedPercent, SelectedTicker.IsIgnored);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    [RelayCommand]
    private async Task SaveAllTickersAsync()
    {
        try
        {
            foreach (var ticker in Tickers)
            {
                await stockService.UpdateTickerAsync(ticker.Name, ticker.ExpectedPercent, ticker.IsIgnored);
            }
            await RefreshTickerDataAsync();
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    private async Task RefreshTickerDataAsync()
    {
        var tickers = (await stockService.GetAllTickersAsync()).ToList();
        var mappedTickers = new List<Ticker>();
        foreach (var ticker in tickers)
        {
            mappedTickers.Add(new Ticker
            {
                Name = ticker.Name,
                ExpectedPercent = ticker.ExpectedPercent ?? decimal.Zero,
                IsIgnored = ticker.IsIgnored == true,
                CurrentPrice = ticker.IsIgnored == true 
                    ? decimal.Zero 
                    : await stockService.GetCurrentPriceAsync(ticker.Name)
            });
        }
        Tickers = new ObservableCollection<Ticker>(mappedTickers);
        ActiveTickers = new ObservableCollection<Ticker>(mappedTickers.Where(t => t.IsIgnored != true));
        TotalNotIgnoredPercent = Math.Round(mappedTickers.Where(t => t.IsIgnored != true).Sum(t => t.ExpectedPercent), 2, MidpointRounding.ToEven);
        TotalNotIgnoredTickers = mappedTickers.Count(t => t.IsIgnored != true);
    }
}
