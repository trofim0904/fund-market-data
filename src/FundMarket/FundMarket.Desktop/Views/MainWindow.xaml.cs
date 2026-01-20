using FundMarket.Desktop.ViewModels;

namespace FundMarket.Desktop.Views;

public partial class MainWindow
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) =>
        {
            await vm.InitializeAsync();
        };
    }
}