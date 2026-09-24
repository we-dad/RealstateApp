using Avalonia.Controls;
using RealEstateInstallmentsManager.Services;
using RealEstateInstallmentsManager.Views;

namespace RealEstateInstallmentsManager;

public partial class MainWindowRealEstate : UserControl
{
    private readonly MainWindow _mainWindow;
    private readonly SupabaseService _supabaseService;

    public MainWindowRealEstate(MainWindow mainWindow, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _mainWindow = mainWindow;

        // Shows whether the last sync with the cloud worked.
        SyncStatusService.Bind(this, SyncStatusText);

        ContentHost.Content = new DashboardViewRealEstate();
        SetActiveTab(TabDashboard);
    }

    // Highlights the tab for the screen currently shown, and only that one.
    private void SetActiveTab(Button active)
    {
        foreach (var tab in new[] { TabDashboard, TabOwners, TabTenants, TabUnits, TabContracts, TabReceipts, TabExpenses })
            tab.Classes.Set("active", tab == active);
    }

    private void Nav_Home(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _mainWindow.ShowMainMenu();
    }

    private void Nav_Dashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new DashboardViewRealEstate();
        SetActiveTab(TabDashboard);
    }

    private void Nav_Owners(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new OwnersViewRealEstate(_supabaseService);
        SetActiveTab(TabOwners);
    }

    private void Nav_Units(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new UnitsViewRealEstate(_supabaseService);
        SetActiveTab(TabUnits);
    }

    private void Nav_Tenants(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new TenantsViewRealEstate(_supabaseService);
        SetActiveTab(TabTenants);
    }

    private void Nav_Contracts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ContractsViewRealEstate(_supabaseService);
        SetActiveTab(TabContracts);
    }

    private void Nav_Receipts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ReceiptsViewRealEstate(_supabaseService);
        SetActiveTab(TabReceipts);
    }

    private void Nav_Expenses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ExpensesViewRealEstate(_supabaseService);
        SetActiveTab(TabExpenses);
    }
}
