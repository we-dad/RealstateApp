using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class MainMenuView : UserControl
{
    private readonly MainWindow _mainWindow;
    public string AppVersion => $"Version {AppVersionService.GetVersion()}";
    public bool IsTester => AppSession.IsTester;

    public MainMenuView(MainWindow mainWindow, SupabaseService supabaseService)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        DataContext = this;

        TopBar.Init(mainWindow, supabaseService);

        var now = DateTime.Now;
        DateText.Text = ArabicDateService.FormatFullDate(now);
        var name = string.IsNullOrWhiteSpace(AppSession.DisplayName) ? "" : $"، {AppSession.DisplayName}";
        GreetingText.Text = $"{ArabicDateService.Greeting(now)}{name}. وش نفتح اليوم؟";

        LoadInstallmentStats();
    }

    private void LoadInstallmentStats()
    {
        try
        {
            var db = new DbServiceInstallment();
            db.Initialize();
            var dashboard = new DashboardServiceInstallment(db);
            var stats = dashboard.GetStats();

            StatActiveContracts.Text = stats.ActiveContracts.ToString();
            StatDueSoon.Text = stats.Upcoming.Count.ToString();

            var arrears = 0.0;
            foreach (var p in stats.LatePayers) arrears += p.ShortfallAmount;
            StatArrears.Text = arrears.ToString("N2");
        }
        catch (Exception ex)
        {
            // Local-DB read failure shouldn't block the module picker itself -
            // show "—" (same convention as the real-estate card's placeholders)
            // instead of leaving blank boxes that could look like real zeros.
            Console.WriteLine(ex.ToString());
            StatActiveContracts.Text = "—";
            StatDueSoon.Text = "—";
            StatArrears.Text = "—";
        }
    }

    private void InstallmentButton_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowInstallmentPage();
    }

    private void RealEstateButton_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow.ShowRealEstatePage();
    }
}
