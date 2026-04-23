using Avalonia.Controls;
using RealEstateInstallmentsManager.Views;

namespace RealEstateInstallmentsManager;

public partial class MainWindow : Window
{
   public MainWindow()
    {
        InitializeComponent();
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        MainContent.Content = new MainMenuView(this);
    }

    public void ShowRealEstatePage()
    {
        MainContent.Content = new MainWindowRealEstate(this);
    }

    public void ShowInstallmentPage()
    {
        MainContent.Content = new MainWindowInstallment(this);
    }
   
}
