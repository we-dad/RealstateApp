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

        // The name can be Latin script (e.g. an email's local part like "admin",
        // per the developer's own account - Supabase has no "full_name" set for
        // it). Mixing an LTR word into an RTL sentence with no isolation confuses
        // the Unicode bidi algorithm and can visually reorder the WHOLE sentence,
        // not just the name (caught by the developer: "تجي مقلوبة"). Wrapping it
        // in First Strong Isolate/Pop Directional Isolate (U+2068/U+2069) tells
        // the renderer to treat the name as its own self-contained run, using
        // whatever direction ITS OWN content actually is, without letting that
        // leak into the surrounding Arabic text's ordering.
        var name = string.IsNullOrWhiteSpace(AppSession.DisplayName)
            ? ""
            : $"، ⁨{AppSession.DisplayName}⁩";
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
