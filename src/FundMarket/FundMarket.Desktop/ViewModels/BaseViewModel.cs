using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace FundMarket.Desktop.ViewModels;

public abstract class BaseViewModel(ILogger logger) : ObservableObject
{
    protected void HandleError(Exception ex)
    {
        const string? errorOccured = "An error occured.";
        logger.LogError(ex, errorOccured);
        MessageBox.Show(
            "Something went wrong while processing the request.",
            "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
