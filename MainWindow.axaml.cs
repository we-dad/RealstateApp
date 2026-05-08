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
        ShowMainMenu();
        _ = StartupAsync();
        
        Opened += async (_, _) =>
        {
            await Task.Delay(3000);
            await Program.CheckForUpdatesWithUI();
        };

    }
    private async Task StartupAsync()
    {
        await _supabaseService.InitializeAsync();

        var auth = new AuthService(_supabaseService);
        var ok = await auth.SignInAsync("weedox1997@gmail.com", "WwW121212");

        Console.WriteLine(ok ? "Login success" : "Login failed");
    }
    public void ShowMainMenu()
    {
        MainContent.Content = new MainMenuView(this);
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
