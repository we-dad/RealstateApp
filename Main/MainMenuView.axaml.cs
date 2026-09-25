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
        // it). See the comment on the GreetingSuffixText/.../GreetingPrefixText
        // TextBlocks in the .axaml: this used to be one string with the name
        // embedded inline, which the Unicode bidi algorithm could visually
        // reorder as a whole (caught by the developer), and wrapping the name in
        // invisible bidi isolate marks (the usual fix) rendered as literal "?"
        // glyphs in this font/engine instead - so instead of fighting bidi with
        // more Unicode tricks, the sentence is split into 3 separate TextBlocks,
        // each holding pure, single-direction text.
        // No trailing/leading spaces embedded here - the StackPanel's own
        // Spacing="4" provides the visible gaps between these 3 TextBlocks;
        // a space at the very edge of one TextBlock's text gets trimmed when
        // there's a separate adjacent control right next to it.
        var hasName = !string.IsNullOrWhiteSpace(AppSession.DisplayName);
        var greeting = ArabicDateService.Greeting(now);
        GreetingPrefixText.Text = hasName ? $"{greeting}" : greeting;
        GreetingNameText.Text = hasName ? AppSession.DisplayName : "";

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
