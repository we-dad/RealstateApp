using Avalonia.Controls;
using RealEstateApp.Views;

namespace RealEstateApp;

public partial class RealStateMainWindow : UserControl
{
        private readonly MainWindow? _mainWindow;

    public RealStateMainWindow()
    {
        InitializeComponent();

        // صفحة البداية
        ContentHost.Content = new DashboardView();
    }

 private void Nav_Home(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_mainWindow is null) return;
        ContentHost.Content = new MainMenuView();

    }

    private void Nav_Dashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new DashboardView();

    private void Nav_Owners(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new OwnersView();

    private void Nav_Units(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new UnitsView();

    private void Nav_Tenants(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new TenantsView();

    private void Nav_Contracts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ContractsView();

    private void Nav_Receipts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ReceiptsView();

    private void Nav_Expenses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   => ContentHost.Content = new ExpensesView();
}
