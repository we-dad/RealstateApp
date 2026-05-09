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
        ShowLogin();
        Opened += async (_, _) =>
        {
            await Task.Delay(3000);
            await Program.CheckForUpdatesWithUI();
        };

    }
    public void ShowLogin()
    {
        MainContent.Content = new LoginView(this, _supabaseService);
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
