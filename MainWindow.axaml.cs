using Avalonia.Controls;
using RealEstateApp.Views;

namespace RealEstateApp;

public partial class MainWindow : Window
{
   public MainWindow()
    {
        InitializeComponent();
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        MainContent.Content = new MainMenuView();
    }

    public void ShowRealEstatePage()
    {
        MainContent.Content = new RealStateMainWindow();
    }

    public void ShowInstallmentPage()
    {
        MainContent.Content = new OwnersView();
    }
   
}
