using Avalonia.Controls;
using RealEstateApp.Views;

namespace RealEstateApp;

public partial class InstallmentMainWindow : UserControl
{
    private readonly MainWindow _mainWindow;

    public InstallmentMainWindow(MainWindow mainWindow)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        // صفحة البداية
        ContentHost.Content = new InstallmentDashboardView();
    }

    private void Nav_Home(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _mainWindow.ShowMainMenu();
    }

    private void Nav_Dashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new InstallmentDashboardView();

    private void Nav_Owners(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new OwnersViewInstallment();

    private void Nav_Products(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ProductViewInstallment();

    private void Nav_Customer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new CustomerViewInstallment();

    private void Nav_Contracts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ContractViewInstallment();

    private void Nav_Receipts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ReceiptViewInstallment();

    private void Nav_Expenses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ExpensesViewInstallment();
}