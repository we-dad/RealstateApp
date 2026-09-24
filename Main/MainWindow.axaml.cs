using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using RealEstateInstallmentsManager.Services;
using RealEstateInstallmentsManager.Views;

namespace RealEstateInstallmentsManager;

public partial class MainWindow : Window
{
    private readonly SupabaseService _supabaseService = new SupabaseService();

   public MainWindow()
    {
        InitializeComponent();
        _ = StartupAsync();
        Opened += async (_, _) =>
        {
            await Task.Delay(3000);
            await Program.CheckForUpdatesWithUI();
        };

    }

    // Shows a brief neutral splash while checking for a saved "تذكرني" session,
    // instead of showing the full login form and then immediately navigating
    // away from it if one is found - that flash (form appears, then instantly
    // replaced) was confusing (caught by the developer testing the feature).
    private async Task StartupAsync()
    {
        MainContent.Content = new SplashView();

        var restored = false;
        try
        {
            await _supabaseService.InitializeAsync();
            if (await _supabaseService.TryRestoreSessionAsync())
            {
                await new AuthService(_supabaseService).PopulateAppSessionAsync();
                restored = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        if (restored) ShowMainMenu(); else ShowLogin();
    }

    public void ShowLogin()
    {
        MainContent.Content = new LoginView(this, _supabaseService);
    }

    public void ShowMainMenu()
    {
        MainContent.Content = new MainMenuView(this, _supabaseService);
    }

    public void ShowRealEstatePage()
    {
        MainContent.Content = new MainWindowRealEstate(this,_supabaseService );
    }

    public void ShowInstallmentPage()
    {
        MainContent.Content = new MainWindowInstallment(this,_supabaseService);
    }
   
}
