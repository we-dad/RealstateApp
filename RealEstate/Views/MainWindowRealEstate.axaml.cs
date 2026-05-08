using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using RealEstateInstallmentsManager.Services;
using RealEstateInstallmentsManager.Views;

namespace RealEstateInstallmentsManager;

public partial class MainWindowRealEstate : UserControl
{
    private readonly MainWindow _mainWindow;
    private readonly SupabaseService _supabaseService;

    public MainWindowRealEstate(MainWindow mainWindow,SupabaseService supabaseService)
    {
        InitializeComponent();
_supabaseService  = supabaseService;

        _mainWindow = mainWindow;
        ContentHost.Content = new DashboardViewRealEstate();
    }

 private void Nav_Home(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _mainWindow.ShowMainMenu();
    }

    private void Nav_Dashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new DashboardViewRealEstate();

    private void Nav_Owners(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new OwnersViewRealEstate(_supabaseService);

    private void Nav_Units(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new UnitsViewRealEstate(_supabaseService);

    private void Nav_Tenants(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new TenantsViewRealEstate(_supabaseService);

    private void Nav_Contracts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ContractsViewRealEstate(_supabaseService);

    private void Nav_Receipts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ContentHost.Content = new ReceiptsViewRealEstate(_supabaseService);

    private void Nav_Expenses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
   => ContentHost.Content = new ExpensesViewRealEstate(_supabaseService);
}
