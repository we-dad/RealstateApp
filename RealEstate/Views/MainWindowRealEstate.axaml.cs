using Avalonia.Controls;
using RealEstateInstallmentsManager.Views;

namespace RealEstateInstallmentsManager;

public partial class MainWindowRealEstate : UserControl
{
    private readonly MainWindow _mainWindow;

    public MainWindowRealEstate(MainWindow mainWindow)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        // صفحة البداية
        ContentHost.Content = new DashboardViewRealEstate();
    }

 private void Nav_Home(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _mainWindow.ShowMainMenu();
    }

    private void Nav_Dashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new DashboardViewRealEstate();

    private void Nav_Owners(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new OwnersViewRealEstate();

    private void Nav_Units(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new UnitsViewRealEstate();

    private void Nav_Tenants(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new TenantsViewRealEstate();

    private void Nav_Contracts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ContractsViewRealEstate();

    private void Nav_Receipts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ReceiptsViewRealEstate();

    private void Nav_Expenses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   => ContentHost.Content = new ExpensesViewRealEstate();
}
