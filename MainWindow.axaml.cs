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
        MainContent.Content = new MainMenuView(this);
    }

    public void ShowRealEstatePage()
    {
        MainContent.Content = new RealStateMainWindow(this);
    }

    public void ShowInstallmentPage()
    {
        MainContent.Content = new InstallmentMainWindow(this);
    }
   
}
