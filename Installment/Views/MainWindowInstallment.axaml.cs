using Avalonia.Controls;
using RealEstateInstallmentsManager.Services;
using RealEstateInstallmentsManager.Views;

namespace RealEstateInstallmentsManager;

public partial class MainWindowInstallment : UserControl
{
    private readonly MainWindow _mainWindow;
    private readonly SupabaseService _supabaseService;

    public MainWindowInstallment(MainWindow mainWindow, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _mainWindow = mainWindow;

        // Shared top bar (brand/identity/connectivity/logout) - same one used
        // on the module picker.
        TopBar.Init(mainWindow, supabaseService);

        // NavLinksPanel is declared in its own row in the XAML only so its
        // Buttons keep plain x:Name/Click wiring; it actually belongs inside
        // TopBar's dark bar (Button.navBtn/.navHome are styled for that dark
        // background), so it's moved there now, once, before the screen is shown.
        // NOTE: this cast assumes NavLinksPanel's direct XAML parent is the root
        // Grid - do not wrap it in another container (e.g. a Border) without
        // updating this line, or it will throw here on startup.
        var outerGrid = (Grid)NavLinksPanel.Parent!;
        outerGrid.Children.Remove(NavLinksPanel);
        TopBar.NavSlot.Content = NavLinksPanel;

        // Shows whether the last sync with the cloud worked. This TextBlock now
        // sits on the light page background, not the old dark nav bar.
        SyncStatusService.Bind(this, SyncStatusText, onDarkBackground: false);

        // صفحة البداية
        ContentHost.Content = new DashboardViewInstallment();
        SetActiveTab(TabDashboard);
    }

    // Highlights the tab for the screen currently shown, and only that one.
    private void SetActiveTab(Button active)
    {
        foreach (var tab in new[] { TabDashboard, TabOwners, TabCustomer, TabProducts, TabContracts, TabReceipts, TabExpenses })
            tab.Classes.Set("active", tab == active);
    }

    private void Nav_Home(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _mainWindow.ShowMainMenu();
    }

    private void Nav_Dashboard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new DashboardViewInstallment();
        SetActiveTab(TabDashboard);
    }

    private void Nav_Owners(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new OwnersViewInstallment(_supabaseService);
        SetActiveTab(TabOwners);
    }

    private void Nav_Products(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ProductViewInstallment(_supabaseService);
        SetActiveTab(TabProducts);
    }

    private void Nav_Customer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new CustomerViewInstallment(_supabaseService);
        SetActiveTab(TabCustomer);
    }

    private void Nav_Contracts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ContractViewInstallment(_supabaseService);
        SetActiveTab(TabContracts);
    }

    private void Nav_Receipts(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ReceiptViewInstallment(_supabaseService);
        SetActiveTab(TabReceipts);
    }

    private void Nav_Expenses(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ContentHost.Content = new ExpensesViewInstallment(_supabaseService);
        SetActiveTab(TabExpenses);
    }
}
